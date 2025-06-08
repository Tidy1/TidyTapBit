using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using TidyTrader.ApiIntegration.LadderStrategy;
using TidyTrader.ApiIntegration.Models.Responses.Trade;

namespace TidyTrader.ApiIntegration.Models
{
    public class GridOrderManager
    {
        // ── EXISTING FIELDS ──
        private readonly BitunixApiClient _client;
        private readonly BitunixWebSocketClient _webSocketClient;
        private readonly BitunixWebSocketClient _webSocketClientPrivate;
        private readonly LiveCapitalManager _capitalManager;
        private readonly Dictionary<string, GridConfig> _configBySymbol;
        private readonly ConcurrentDictionary<string, GridOrder> _activeOrders = new();
        private readonly Dictionary<string, List<decimal>> _longRungs = new();
        private readonly Dictionary<string, List<decimal>> _shortRungs = new();
        private readonly ILadderStrategy _ladder;
        private readonly OrderServiceAdapter _orderServiceAdapter;

        // ── A per‐symbol lock to serialize any “initial grid” / “re-init” logic ──
        private static readonly ConcurrentDictionary<string, SemaphoreSlim> _symbolLocks =
            new ConcurrentDictionary<string, SemaphoreSlim>();
        public static SemaphoreSlim GetSymbolLock(string symbol) =>
            _symbolLocks.GetOrAdd(symbol, _ => new SemaphoreSlim(1, 1));

        // ── Replace the old “_isGridInitialized” with a single atomic “_initialized” set ──
        private readonly ConcurrentDictionary<string, bool> _initialized = new();
        private readonly ConcurrentDictionary<string, DateTime> _lastInitTime = new();

        // ── FIELDS FOR KLINE + FUNDING STREAMS ──
        private readonly ConcurrentDictionary<string, FixedSizeQueue<Candle>> _klineBuffers =
            new ConcurrentDictionary<string, FixedSizeQueue<Candle>>();

        private decimal _latestFundingRate = 0m;
        private readonly object _fundingLock = new object();

        private const int AtrPeriod = 14;
        private DateTime _lastCapitalRefreshTime = DateTime.MinValue;

        private decimal _liveUsdtAvailable = 0m;
        private readonly object _usdtLock = new();

        /// <summary>
        /// Expose rung lists publicly so Program.cs can inspect/realign if needed.
        /// </summary>
        public IReadOnlyDictionary<string, List<decimal>> LongRungs => _longRungs;
        public IReadOnlyDictionary<string, List<decimal>> ShortRungs => _shortRungs;



        private class Candle
        {
            public DateTime CloseTimeUtc { get; set; }
            public decimal Open { get; set; }
            public decimal High { get; set; }
            public decimal Low { get; set; }
            public decimal Close { get; set; }
        }

        private class FixedSizeQueue<T>
        {
            private readonly int _maxSize;
            private readonly LinkedList<T> _list = new LinkedList<T>();

            public FixedSizeQueue(int maxSize) => _maxSize = maxSize;

            public void Enqueue(T item)
            {
                _list.AddLast(item);
                if (_list.Count > _maxSize)
                    _list.RemoveFirst();
            }

            public IList<T> ToList() => _list.ToList();
            public int Count => _list.Count;
        }

        public GridOrderManager(
            BitunixApiClient client,
            BitunixWebSocketClient webSocketClient,
            BitunixWebSocketClient webSocketClientPrivate,
            LiveCapitalManager capitalManager,
            Dictionary<string, GridConfig> configBySymbol,
            ILadderStrategy ladder,
            OrderServiceAdapter orderServiceAdapter)
        {
            _client = client;
            _webSocketClient = webSocketClient;
            _webSocketClientPrivate = webSocketClientPrivate;
            _capitalManager = capitalManager;
            _configBySymbol = configBySymbol;
            _ladder = ladder;
            _orderServiceAdapter = orderServiceAdapter;

            // Initialize your rung‐lists for every symbol  
            foreach (var sym in _configBySymbol.Keys)
            {
                _longRungs[sym] = new List<decimal>();
                _shortRungs[sym] = new List<decimal>();
            }


            // 1) Handle OnOrderUpdate for CREATE / FILLED / CANCELED
            _webSocketClientPrivate.OnOrderUpdate += updates =>
            {
                foreach (var o in updates)
                {
                    string ev = o.Event;       // "CREATE", "FILLED", or "CANCEL"
                    string id = o.OrderId;
                    string sym = o.Symbol;

                    if (!decimal.TryParse(o.Price, NumberStyles.Any, CultureInfo.InvariantCulture, out var price))
                        continue;
                    if (!decimal.TryParse(o.Qty, NumberStyles.Any, CultureInfo.InvariantCulture, out var qty))
                        continue;

                    decimal leverage = _configBySymbol[sym].Leverage;
                    decimal margin = (price * qty) / leverage;

                    if (ev == "CREATE")
                    {
                        // Optionally reserve margin here if you prefer:
                        // _capitalManager.ReserveMargin(id, margin);
                    }
                    else if (ev == "FILLED" || ev == "CANCELED")
                    {
                        // a) Release margin that was reserved for order “id”
                        _capitalManager.ReleaseMargin(id);

                        // b) Remove it from activeOrders, and from rung lists
                        if (_activeOrders.TryRemove(id, out var removed))
                        {
                            Console.WriteLine($"[ON-ORDER-UPDATE] Removed order {id}. Active orders for {sym}: {_activeOrders.Values.Count(o => o.Symbol == sym)}");
                            _longRungs[sym].Remove(removed.EntryPrice);
                            _shortRungs[sym].Remove(removed.EntryPrice);

                            _ = CleanupExtraOrdersAsync(sym)
                            .ContinueWith(t =>
                            {
                                // swallow/log any error
                                if (t.Exception != null)
                                    Console.WriteLine($"Cleanup failed: {t.Exception}");
                                // re-sort after cleanup
                                _longRungs[sym].Sort();
                                _shortRungs[sym].Sort();
                            }, TaskScheduler.Default);
                        }

                    }
                }
            };

            // in your constructor, after assigning _ladder…

            // 1) on every FILLED order → tell the ladder
            _webSocketClientPrivate.OnOrderUpdate += async updates =>
            {
                foreach (var o in updates.Where(u => u.Event == "FILLED"))
                {
                    var side = o.Side == "BUY" ? OrderSide.Long : OrderSide.Short;
                    var price = decimal.Parse(o.Price, CultureInfo.InvariantCulture);

                    // 1a) let your IOrderService know about it (so it can detect TP vs SL)
                    _orderServiceAdapter.NotifyFill(o.OrderId, price);

                    // 1b) drive the ladder
                    await _ladder.OnOrderFilledAsync(o.OrderId, side, price);
                }
                // your existing “CANCELED” logic can stay as-is
            };

            // 2) on every market tick → give it to the ladder
            _webSocketClient.OnPriceUpdate += async update =>
            {
                var live = decimal.Parse(update.Data.MarketPrice, CultureInfo.InvariantCulture);
                await _ladder.OnPriceTickAsync(live);
            };


            // 2) Handle funding‐rate updates
            _webSocketClientPrivate.OnFundingUpdate += fr =>
            {
                lock (_fundingLock)
                {
                    _latestFundingRate = fr;
                }
            };

            // 3) Handle closed 1m‐kline updates for ATR
            _webSocketClient.OnKlineUpdate += json =>
            {
                try
                {
                    var payload = JObject.Parse(json);
                    var k = payload["data"]?["k"];
                    if (k == null) return;
                    bool isFinal = k.Value<bool>("x");
                    if (!isFinal) return;

                    var sym = k.Value<string>("s");
                    var candle = new Candle
                    {
                        CloseTimeUtc = DateTimeOffset
                            .FromUnixTimeMilliseconds(k.Value<long>("T")).UtcDateTime,
                        Open = decimal.Parse(k.Value<string>("o"), CultureInfo.InvariantCulture),
                        High = decimal.Parse(k.Value<string>("h"), CultureInfo.InvariantCulture),
                        Low = decimal.Parse(k.Value<string>("l"), CultureInfo.InvariantCulture),
                        Close = decimal.Parse(k.Value<string>("c"), CultureInfo.InvariantCulture)
                    };

                    if (!_klineBuffers.TryGetValue(sym, out var queue))
                    {
                        queue = new FixedSizeQueue<Candle>(AtrPeriod + 1);
                        _klineBuffers[sym] = queue;
                    }

                    queue.Enqueue(candle);
                }
                catch
                {
                    // swallow parsing exceptions
                }
            };

            _webSocketClientPrivate.OnBalanceUpdate += json =>
            {
                try
                {
                    if (decimal.TryParse(json.First().Available, NumberStyles.Any, CultureInfo.InvariantCulture, out var avail))
                    {
                        // 1) Update our local _liveUsdtAvailable
                        lock (_usdtLock)
                        {
                            _liveUsdtAvailable = avail;
                        }

                        // 2) Overwrite the manager’s TOTAL-CAPITAL to whatever the exchange says “available USDT” is,
                        //    plus whatever margin is still “allocated” internally.
                        decimal currentlyAllocated = _capitalManager.Allocated;
                        _capitalManager.RefreshTotalCapital(avail);
                    }
                }
                catch
                {
                    // ignore any parsing issues
                }
            };


            _ladder = ladder;
        }

        private decimal GetFreeUsdt() =>
            _capitalManager.Available;

        public bool HasAnyActiveOrdersForSymbol(string symbol) =>
            _activeOrders.Values.Any(o => o.Symbol.Equals(symbol, StringComparison.OrdinalIgnoreCase));

        public bool HasOrder(string orderId) => _activeOrders.ContainsKey(orderId);

        /// <summary>
        /// Attempt to claim the “first grid” for this symbol.
        /// Returns true exactly once per symbol, false thereafter.
        /// </summary>
        public bool TryClaimFirstGrid(string symbol)
        {
            // TryAdd returns true if key was absent, false if it was already present.
            if (_initialized.TryAdd(symbol, true))
            {
                _lastInitTime[symbol] = DateTime.UtcNow;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Clear the “initialized” flag (so you can re‐initialize on purpose).
        /// </summary>
        public void ClearGridInitialized(string symbol)
        {
            _initialized.TryRemove(symbol, out _);
        }

        /// <summary>
        /// Whether we can re‐initialize after a cooldown (e.g. 10 seconds).
        /// </summary>
        public bool CanInitialize(string symbol, TimeSpan cooldown)
        {
            var now = DateTime.UtcNow;
            if (_lastInitTime.TryGetValue(symbol, out var last) && now - last < cooldown)
                return false;

            _lastInitTime[symbol] = now;
            return true;
        }

        // ── GRID INITIALIZATION METHODS ──

        public async Task InitializeGridAsync(string symbol, decimal maxCapitalPerBot, GridConfig config)
        {
            // 1) Hook our rung-update event so _longRungs/_shortRungs stay in sync
            _ladder.OnRungsUpdated += rungs =>
            {
                // clear out the old lists
                _longRungs[symbol].Clear();
                _shortRungs[symbol].Clear();

                // repopulate from the ladder
                foreach (var r in rungs)
                {
                    if (r.Side == OrderSide.Long)
                        _longRungs[symbol].Add(r.Price);
                    else
                        _shortRungs[symbol].Add(r.Price);
                }

                Console.WriteLine($"[{symbol}] New grid ←");
                Console.WriteLine($"  Longs:  {string.Join(", ", _longRungs[symbol])}");
                Console.WriteLine($"  Shorts: {string.Join(", ", _shortRungs[symbol])}");
            };

            // 2) Grab our very first live price
            var maybePrice = _webSocketClient.GetLatestPrice(symbol);
            if (!maybePrice.HasValue)
            {
                Console.WriteLine($"[{symbol}] Cannot init ladder: no price yet");
                return;
            }
            decimal initialPrice = maybePrice.Value;

            Console.WriteLine($"[{symbol}] INITIALIZING ladder @ {initialPrice:F6}");

            // 3) Fire off the ladder seed
            await _ladder.InitializeAsync(initialPrice);

            // 4) Mark it so no one else does it
            TryClaimFirstGrid(symbol);
        }


        public async Task InitializeSmartStraddleGridAsync(string symbol, decimal capital, GridConfig config)
        {
            var sem = GetSymbolLock(symbol);
            await sem.WaitAsync();
            try
            {
                // Only run once per symbol
                if (!TryClaimFirstGrid(symbol))
                    return;
            }
            finally
            {
                sem.Release();
            }
            // 1) Gather candles for ATR
            var (highs, lows, closes) = GetLatestKlineArrays(symbol);
            if (closes.Count < AtrPeriod + 1)
            {
                Console.WriteLine($"[{symbol}] SKIP: Not enough candles.");
                return;
            }

            // 2) Compute spacing
            decimal atr = CalculateAtr(AtrPeriod, highs, lows, closes);
            decimal spacing = atr * config.AtrMultipler;

            // 3) Fetch base price (WS first, REST fallback)
            decimal basePrice;
            if (_webSocketClient.GetLatestPrice(symbol) is decimal wsPrice)
            {
                basePrice = wsPrice;
            }
            else
            {
                var tickerData = (await _client.GetTickersAsync(symbol))?.Data?.Data.FirstOrDefault();
                if (tickerData == null || string.IsNullOrWhiteSpace(tickerData.LastPrice))
                {
                    Console.WriteLine($"[{symbol}] ERROR: Fallback price fetch failed.");
                    return;
                }
                basePrice = decimal.Parse(tickerData.LastPrice, CultureInfo.InvariantCulture);
            }

            // 4) Reset rung lists
            _longRungs[symbol] = new List<decimal>();
            _shortRungs[symbol] = new List<decimal>();

            // 5) Place BUY ladder below basePrice
            for (int i = 1; i <= config.LongOrderCount; i++)
            {
                decimal price = basePrice - spacing * i;
                var orderId = await PlaceSingleGridOrder(symbol, price, "BUY", config, capital);
                if (orderId != null)
                {
                    _longRungs[symbol].Add(price);
                    Console.WriteLine($"[{symbol}] Straddle BUY placed @ {price:F4}");
                }
                await Task.Delay(250);
            }

            // 6) Place SELL ladder above basePrice
            for (int i = 1; i <= config.ShortOrderCount; i++)
            {
                decimal price = basePrice + spacing * i;
                var orderId = await PlaceSingleGridOrder(symbol, price, "SELL", config, capital);
                if (orderId != null)
                {
                    _shortRungs[symbol].Add(price);
                    Console.WriteLine($"[{symbol}] Straddle SELL placed @ {price:F4}");
                }
                await Task.Delay(250);
            }

            Console.WriteLine($"[{symbol}] Smart straddle initialized at {basePrice:F4} (spacing={spacing:F6})");

        }


        // ── STREAM SUBSCRIPTION METHODS ──

        public async Task StartKlineStreamAsync(string symbol)
        {
            _klineBuffers.TryAdd(symbol, new FixedSizeQueue<Candle>(AtrPeriod + 1));
            await _webSocketClient.SubscribeToKlineAsync(symbol, "1m");
        }

        public async Task StartFundingRateStreamAsync(string symbol)
        {
            await _webSocketClient.SubscribeToFundingRateAsync(symbol);
        }

        // ── CORE GRID LOGIC ──

        public decimal CalculateAtr(int period, List<decimal> highs, List<decimal> lows, List<decimal> closes)
        {
            var trs = new List<decimal>();
            for (int i = 1; i < highs.Count; i++)
            {
                decimal tr1 = highs[i] - lows[i];
                decimal tr2 = Math.Abs(highs[i] - closes[i - 1]);
                decimal tr3 = Math.Abs(lows[i] - closes[i - 1]);
                trs.Add(Math.Max(tr1, Math.Max(tr2, tr3)));
            }

            if (trs.Count < period)
                return trs.Count == 0 ? 0m : trs.Average();

            decimal sum = trs.Skip(trs.Count - period).Take(period).Sum();
            return sum / period;
        }

        public (List<decimal> highs, List<decimal> lows, List<decimal> closes) GetLatestKlineArrays(string symbol)
        {
            if (!_klineBuffers.TryGetValue(symbol, out var buffer) || buffer.Count < AtrPeriod + 1)
                return (new List<decimal>(), new List<decimal>(), new List<decimal>());

            var candles = buffer.ToList();
            var highs = candles.Select(c => c.High).ToList();
            var lows = candles.Select(c => c.Low).ToList();
            var closes = candles.Select(c => c.Close).ToList();
            return (highs, lows, closes);
        }

        public async Task<string?> PlaceSingleGridOrder(
            string symbol,
            decimal entryPrice,
            string side,            // "BUY" or "SELL"
            GridConfig config,
            decimal maxCapitalPerBot)
        {
            // 1) Count existing same‐side orders
            int existingSameSideCount = _activeOrders.Values
                .Count(o => o.Symbol == symbol && o.Side == side);
            if ((side == "BUY" && existingSameSideCount >= config.LongOrderCount) ||
                (side == "SELL" && existingSameSideCount >= config.ShortOrderCount))
            {
                return null;
            }

            // 2) Count total open orders for this symbol
            int totalOpen = _activeOrders.Values.Count(o => o.Symbol == symbol);
            if (totalOpen >= config.MaxBotsPerSymbol)
            {
                return null;
            }

            // 3) RISK CHECK: get current price
            var maybePrice = _webSocketClient.GetLatestPrice(symbol);
            if (!maybePrice.HasValue)
            {
                return null;
            }
            decimal currentPrice = maybePrice.Value;

            // 3.a) compute unrealized PnL for this side
            decimal unrealizedSidePnl = CalculateUnrealizedPnL(symbol, side, currentPrice);
            if (unrealizedSidePnl < -config.MaxLossPerSideUsd)
            {
                Console.WriteLine($"[RISK] {symbol} {side} PnL=${unrealizedSidePnl:F2} < –${config.MaxLossPerSideUsd:F2}. Skipping new {side}.");
                return null;
            }

            // 3.b) percent‐based check
            if (config.MaxLossPerSidePct > 0m)
            {
                var sideOrders = _activeOrders.Values
                    .Where(o => o.Symbol == symbol && o.Side == side)
                    .ToList();
                if (sideOrders.Any())
                {
                    decimal avgEntry = sideOrders.Average(o => o.EntryPrice);
                    decimal pctDown = (avgEntry - currentPrice) / avgEntry;
                    if (pctDown >= config.MaxLossPerSidePct)
                    {
                        Console.WriteLine($"[RISK] {symbol} {side} is down {pctDown:P2} ≥ {config.MaxLossPerSidePct:P2}. No new {side}.");
                        return null;
                    }
                }
            }

            Console.WriteLine($"[DEBUG] Trying {side} @ {entryPrice:F4} freeUsdt={GetFreeUsdt():F2} existingBuys=");

            // 4) LIVE BALANCE CHECK: now uses in-memory capital manager
            decimal freeUsdt = GetFreeUsdt();
            decimal qty = Math.Round((maxCapitalPerBot * config.Leverage) / entryPrice, 4);
            if (qty < 0.0001m)
            {
                Console.WriteLine($"[ERROR] Qty too small for {symbol} @ {entryPrice:F4}: qty={qty}");
                return null;
            }
            decimal margin = (entryPrice * qty) / config.Leverage;

            if (freeUsdt < margin)
            {
                Console.WriteLine($"[SKIP] {symbol}: available capital ${freeUsdt:F2} < required margin ${margin:F2}");
                return null;
            }


            // Re-count inside lock for safety
            totalOpen = _activeOrders.Values.Count(o => o.Symbol == symbol);
            if (totalOpen >= config.MaxBotsPerSymbol)
                return null;

            existingSameSideCount = _activeOrders.Values
                .Count(o => o.Symbol == symbol && o.Side == side);
            if ((side == "BUY" && existingSameSideCount >= config.LongOrderCount) ||
                (side == "SELL" && existingSameSideCount >= config.ShortOrderCount))
            {
                return null;
            }

            // 6) Compute ATR → TP/SL
            var (highs, lows, closes) = GetLatestKlineArrays(symbol);
            if (closes.Count < AtrPeriod + 1)
                return null;

            decimal atr = CalculateAtr(AtrPeriod, highs, lows, closes);
            decimal takeAbs = atr * config.TakeProfitPct;
            decimal stopAbs = atr * config.StopLossPct;

            decimal fundingAdj;
            lock (_fundingLock)
            {
                fundingAdj = _latestFundingRate;
            }

            decimal tpPrice = (side == "BUY")
                ? entryPrice + takeAbs + fundingAdj
                : entryPrice - takeAbs - fundingAdj;
            decimal slPrice = (side == "BUY")
                ? entryPrice - stopAbs - fundingAdj
                : entryPrice + stopAbs + fundingAdj;

            if (!config.UseStopLoss)
            {
                decimal wideStop = atr * 5;
                slPrice = (side == "BUY")
                    ? entryPrice - wideStop
                    : entryPrice + wideStop;
            }
            if (config.EnableGroupedTakeProfit)
            {
                tpPrice = (side == "BUY")
                    ? entryPrice + (entryPrice * config.GroupTakeProfitPct)
                    : entryPrice - (entryPrice * config.GroupTakeProfitPct);
            }

            // 7) Place the LIMIT order
            await Task.Delay(150);
            var resp = await _client.PlaceOrderAsync(
                symbol,
                qty.ToString(CultureInfo.InvariantCulture),
                side, "OPEN", "LIMIT",
                entryPrice.ToString(CultureInfo.InvariantCulture),
                "GTC", Guid.NewGuid().ToString(),
                null, false,
                tpPrice.ToString(CultureInfo.InvariantCulture), "MARK_PRICE", "LIMIT",
                tpPrice.ToString(CultureInfo.InvariantCulture),
                slPrice.ToString(CultureInfo.InvariantCulture), "MARK_PRICE", "LIMIT",
                slPrice.ToString(CultureInfo.InvariantCulture)
            );

            if (resp?.Data?.Data?.OrderId != null)
            {
                string orderId = resp.Data.Data.OrderId;

                // a) Reserve margin internally
                try
                {
                    _capitalManager.ReserveMargin(orderId, margin);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[CAPITAL‐ERROR] Could not reserve ${margin:F2} for {orderId}: {ex.Message}");
                    await CancelAndReleaseAsync(symbol, new List<string> { orderId });
                    return null;
                }

                // b) Add to in-memory _activeOrders
                _activeOrders[orderId] = new GridOrder(orderId, margin, entryPrice, symbol, config.Leverage, side);

                Console.WriteLine($"[BOTCOUNT] {symbol}: total open now {_activeOrders.Values.Count(o => o.Symbol == symbol)} (after {side} @ {entryPrice:F4})");
                Console.WriteLine($"[ORDER] {symbol} {side} @ {entryPrice:F4} qty={qty} TP={tpPrice:F4} SL={slPrice:F4}");
                return orderId;
            }
            else
            {
                var dataPayload = resp?.Data;

                if (dataPayload != null && dataPayload.Code == 20003)
                {
                    var newBalance = await _client.GetUsdtAvailableAsync();
                    _capitalManager.RefreshTotalCapital(newBalance);
                }

                Console.WriteLine($"[ERROR] Invalid response for {symbol} {side} @ {entryPrice:F4}");
                Console.WriteLine($"[DEBUG] Payload: {JsonConvert.SerializeObject(dataPayload)}");
                return null;
            }

        }

        private decimal CalculateUnrealizedPnL(string symbol, string side, decimal currentPrice)
        {
            decimal totalPnl = 0m;
            foreach (var gridOrder in _activeOrders.Values
                     .Where(o => o.Symbol == symbol && o.Side == side))
            {
                decimal qty = (gridOrder.CapitalAllocated * gridOrder.Leverage) / gridOrder.EntryPrice;
                if (side == "BUY")
                    totalPnl += (currentPrice - gridOrder.EntryPrice) * qty;
                else
                    totalPnl += (gridOrder.EntryPrice - currentPrice) * qty;
            }
            return totalPnl;
        }

        public async Task CancelGridOrderByPrice(string symbol, decimal entryPrice)
        {
            var toCancel = _activeOrders
                .Where(kv => kv.Value.Symbol == symbol
                             && Math.Abs(kv.Value.EntryPrice - entryPrice) < 0.0001m)
                .Select(kv => kv.Key)
                .ToList();


            await CancelAndReleaseAsync(symbol, toCancel);
        }

        private async Task CancelAndReleaseAsync(string symbol, IEnumerable<string> orderIds)
        {
            var resp = await _client.CancelOrdersAsync(symbol, orderIds.ToList());
            if (!resp.IsSuccessStatusCode) return;
            foreach (var id in orderIds)
            {
                if (_activeOrders.TryRemove(id, out _))
                    _capitalManager.ReleaseMargin(id);
            }
        }

        public async Task CleanupExtraOrdersAsync(string symbol)
        {
            if (!_configBySymbol.TryGetValue(symbol, out var config)) return;

            var maybePrice = _webSocketClient.GetLatestPrice(symbol);
            if (!maybePrice.HasValue) return;
            decimal currentPrice = maybePrice.Value;

            // 1) BUYs sorted by distance descending
            var buyOrders = _activeOrders
                .Where(kv => kv.Value.Symbol == symbol && kv.Value.Side == "BUY")
                .Select(kv => new { kv.Key, kv.Value.EntryPrice })
                .OrderByDescending(x => Math.Abs(x.EntryPrice - currentPrice))
                .ToList();

            int excessBuys = buyOrders.Count - config.LongOrderCount;

            await CancelAndReleaseAsync(symbol, buyOrders.Take(excessBuys).Select(x => x.Key));

            // 2) SELLs sorted by distance descending
            var sellOrders = _activeOrders
                .Where(kv => kv.Value.Symbol == symbol && kv.Value.Side == "SELL")
                .Select(kv => new { kv.Key, kv.Value.EntryPrice })
                .OrderByDescending(x => Math.Abs(x.EntryPrice - currentPrice))
                .ToList();

            int excessSells = sellOrders.Count - config.ShortOrderCount;

            await CancelAndReleaseAsync(symbol, sellOrders.Take(excessSells).Select(x => x.Key));


        }

        public async Task StartOrderMonitorAsync(string symbol, GridConfig config, decimal maxCapitalPerBot)
        {
            const decimal tolerance = 0.0008m; // ~0.05% price‐match tolerance

            // helper to ask “are we already at the 10-bot cap?”
            bool AtCap() =>
                _activeOrders.Values.Count(o => o.Symbol == symbol)
                >= config.MaxBotsPerSymbol;

            // how many rungs we need before trend logic kicks in:
            int requiredRungs = config.LongOrderCount + config.ShortOrderCount;

            while (true)
            {
                var wsPrice = _webSocketClient.GetLatestPrice(symbol);
                if (!wsPrice.HasValue)
                {
                    await Task.Delay(250);
                    continue;
                }
                decimal currentPrice = wsPrice.Value;


                await CleanupExtraOrdersAsync(symbol);

                if (_activeOrders.Values.Count(o => o.Symbol == symbol) < requiredRungs)
                {
                    await Task.Delay(500);
                    continue;
                }

                // 1) Cancel stale + extra
                var stale = _activeOrders
                    .Where(kv =>
                        kv.Value.Symbol == symbol
                        && Math.Abs(kv.Value.EntryPrice - currentPrice) / currentPrice > 0.01m
                        && kv.Value.IsStale(config.StaleAgeSeconds))
                    .Select(kv => kv.Key)
                    .ToList();
                if (stale.Count > 0)
                    await CancelAndReleaseAsync(symbol, stale);

                await CleanupExtraOrdersAsync(symbol);

                // 2) If at cap, skip placements this cycle
                if (AtCap())
                {
                    await Task.Delay(500);
                    continue;
                }

                // 3) Compute spacing
                var (hs, ls, cs) = GetLatestKlineArrays(symbol);
                if (hs.Count < AtrPeriod + 1)
                {
                    await Task.Delay(500);
                    continue;
                }
                decimal atr = CalculateAtr(AtrPeriod, hs, ls, cs);
                decimal spacing = atr * config.AtrMultipler;

                // 4) Replenish missing grid rungs (await each one)
                int existingBuys = _activeOrders.Values.Count(o => o.Symbol == symbol && o.Side == "BUY");
                int existingSells = _activeOrders.Values.Count(o => o.Symbol == symbol && o.Side == "SELL");
                int longsNeeded = Math.Max(0, config.LongOrderCount - existingBuys);
                int shortsNeeded = Math.Max(0, config.ShortOrderCount - existingSells);

                if (!AtCap() && longsNeeded > 0)
                {
                    for (int i = 0; i < config.LongOrderCount && longsNeeded > 0; i++)
                    {
                        decimal price = currentPrice - spacing * (i + 1);
                        bool exists = _activeOrders.Values.Any(o =>
                            o.Symbol == symbol
                            && o.Side == "BUY"
                            && Math.Abs(o.EntryPrice - price) <= tolerance);
                        if (exists || AtCap()) continue;

                        var orderId = await PlaceSingleGridOrder(symbol, price, "BUY", config, maxCapitalPerBot);
                        if (orderId != null)
                        {
                            _longRungs[symbol].Add(price);
                            _longRungs[symbol].Sort();
                            longsNeeded--;
                        }
                    }
                }

                if (!AtCap() && shortsNeeded > 0)
                {
                    for (int i = 0; i < config.ShortOrderCount && shortsNeeded > 0; i++)
                    {
                        decimal price = currentPrice + spacing * (i + 1);
                        bool exists = _activeOrders.Values.Any(o =>
                            o.Symbol == symbol
                            && o.Side == "SELL"
                            && Math.Abs(o.EntryPrice - price) <= tolerance);
                        if (exists || AtCap()) continue;

                        var orderId = await PlaceSingleGridOrder(symbol, price, "SELL", config, maxCapitalPerBot);
                        if (orderId != null)
                        {
                            _shortRungs[symbol].Add(price);
                            _shortRungs[symbol].Sort();
                            shortsNeeded--;
                        }
                    }
                }

                // 5) Trend adjustments—but only once initial grid is fully seeded
                if (_activeOrders.Values.Count(o => o.Symbol == symbol) >= requiredRungs)
                {
                    await AdjustForTrendAsync(symbol, config, currentPrice, spacing, maxCapitalPerBot);
                }

                await Task.Delay(500);
            }
        }


        public async Task MonitorProfitZoneAsync(string symbol, GridConfig config)
        {
            while (true)
            {
                var price = _webSocketClient.GetLatestPrice(symbol);
                if (price == null)
                {
                    await Task.Delay(500);
                    continue;
                }

                var filledOrders = _activeOrders.Values.Where(o => o.Symbol == symbol).ToList();
                if (filledOrders.Count == 0)
                {
                    await Task.Delay(1000);
                    continue;
                }

                decimal avgEntry = filledOrders.Average(o => o.EntryPrice);
                decimal takeProfitThreshold = avgEntry * (1 + config.GroupTakeProfitPct);
                decimal takeProfitThresholdShort = avgEntry * (1 - config.GroupTakeProfitPct);

                bool closeCondition =
                    price.Value >= takeProfitThreshold ||
                    price.Value <= takeProfitThresholdShort;

                if (closeCondition)
                {
                    Console.WriteLine($"[TP-ZONE] Closing all positions for {symbol} @ {price:F4}, avgEntry: {avgEntry:F4}");
                    await CloseAllPositionsAsync(symbol);

                    // Rebuild grid after TP hit
                    await ChaseGridWithAvailableCapitalAsync(symbol, price.Value, config,
                        config.OrderQuantity * price.Value / config.Leverage);
                }

                await Task.Delay(1000);
            }
        }

        public async Task SyncOpenOrdersAsync(string symbol, GridConfig config)
        {
            // 1) Remove only this symbol’s orders from _activeOrders
            var toRemove = _activeOrders.Keys
                .Where(orderId => _activeOrders[orderId].Symbol.Equals(symbol, StringComparison.OrdinalIgnoreCase))
                .ToList();
            foreach (var orderId in toRemove)
                _activeOrders.TryRemove(orderId, out _);

            // 2) Ensure the rung lists exist, then clear them
            if (!_longRungs.ContainsKey(symbol))
                _longRungs[symbol] = new List<decimal>();
            else
                _longRungs[symbol].Clear();

            if (!_shortRungs.ContainsKey(symbol))
                _shortRungs[symbol] = new List<decimal>();
            else
                _shortRungs[symbol].Clear();

            // 3) Fetch pending orders via REST
            var response = await _client.GetPendingOrdersAsync(symbol);
            if (response?.Data?.Data?.OrderList == null)
                return;

            // 4) Rebuild in-memory state from the exchange snapshot
            foreach (var order in response.Data.Data.OrderList)
            {
                string oid = order.OrderId;
                decimal entryPrice = decimal.Parse(order.Price, CultureInfo.InvariantCulture);
                string side = order.Side.ToUpperInvariant();

                if (!_activeOrders.ContainsKey(oid))
                {
                    _activeOrders[oid] = new GridOrder(oid, 0m, entryPrice, symbol, config.Leverage, side);

                    if (side == "BUY" && !_longRungs[symbol].Contains(entryPrice))
                        _longRungs[symbol].Add(entryPrice);
                    else if (side == "SELL" && !_shortRungs[symbol].Contains(entryPrice))
                        _shortRungs[symbol].Add(entryPrice);
                }
            }

            // 5) Sort the rung lists so they remain ordered
            _longRungs[symbol].Sort();
            _shortRungs[symbol].Reverse();
        }

        public void HandleOrderClosed(string symbol, string clientId)
        {
            if (_activeOrders.TryRemove(clientId, out var order))
            {
                _capitalManager.ReleaseMargin(clientId);
                Console.WriteLine($"[Order Closed] {clientId} - Released ${order.CapitalAllocated:F2}");
            }
            else
            {
                Console.WriteLine($"[WARN] Tried to close unknown order {clientId} for {symbol}.");
            }

            Console.WriteLine($"[DEBUG] Active Orders left: {_activeOrders.Count}");
        }

        public async Task StartBotStatusLoggerAsync(string symbol)
        {
            while (true)
            {
                Console.WriteLine("\n================= BOT STATUS REPORT =================");
                Console.WriteLine($"Timestamp (UTC): {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}");
                Console.WriteLine($"[{symbol}][STATUS] OpenOrders={_activeOrders.Count}, PnL={_capitalManager.Available:P4}");

                decimal totalAllocated = _capitalManager.Allocated;
                decimal totalAvailable = _capitalManager.Available;
                decimal totalCapital = totalAllocated + totalAvailable;

                Console.WriteLine($"Total Bots Active: {_activeOrders.Count}");
                Console.WriteLine($"Total Capital: ${totalCapital:F2}");
                Console.WriteLine($"Capital Allocated: ${totalAllocated:F2}");
                Console.WriteLine($"Capital Available: ${totalAvailable:F2}");

                foreach (var kv in _activeOrders)
                {
                    Console.WriteLine($" - Order {kv.Key}: {kv.Value.Side} @ {kv.Value.EntryPrice:F4}");
                }

                Console.WriteLine("=====================================================\n");

                await Task.Delay(TimeSpan.FromMinutes(1));
            }
        }

        public async Task ResetGridAsync(string symbol, string marginCoin, decimal livePrice, GridConfig config, decimal maxCapitalPerBot)
        {
            ClearGridInitialized(symbol);

            // Cancel all active orders for the symbol
            var orderIdsToCancel = _activeOrders
                .Where(kvp => kvp.Value.Symbol == symbol)
                .Select(kvp => kvp.Key)
                .ToList();

            await CancelAndReleaseAsync(symbol, orderIdsToCancel);


            // Rebuild straddle grid at live price
            var (hs, ls, cs) = GetLatestKlineArrays(symbol);
            decimal atr = CalculateAtr(AtrPeriod, hs, ls, cs);
            decimal spacing = atr * config.AtrMultipler;

            // Use spacing instead of PriceSpacingPct
            List<decimal> longs = new();
            List<decimal> shorts = new();

            for (int i = 1; i <= config.LongOrderCount; i++)
                longs.Add(livePrice - spacing * i);

            for (int i = 1; i <= config.ShortOrderCount; i++)
                shorts.Add(livePrice + spacing * i);

            foreach (var longEntry in longs)
            {
                var newOrder = await PlaceSingleGridOrder(symbol, longEntry, "BUY", config, maxCapitalPerBot);
            }

            foreach (var shortEntry in shorts)
            {
                var newOrder = await PlaceSingleGridOrder(symbol, shortEntry, "SELL", config, maxCapitalPerBot);
            }

            _longRungs[symbol] = longs;
            _shortRungs[symbol] = shorts;

            TryClaimFirstGrid(symbol);
        }

        public async Task ChaseGridWithAvailableCapitalAsync(string symbol, decimal livePrice, GridConfig config, decimal maxCapitalPerBot)
        {
            int current = _activeOrders.Values.Count(o => o.Symbol == symbol);
            if (current >= config.MaxBotsPerSymbol)
            {
                Console.WriteLine($"[{symbol}] At max bot count ({current}). Skipping chase.");
                return;
            }

            if (!_longRungs.ContainsKey(symbol) || !_shortRungs.ContainsKey(symbol))
            {
                Console.WriteLine($"[{symbol}] ERROR: Rung data missing.");
                return;
            }

            var longRungs = _longRungs[symbol];
            var shortRungs = _shortRungs[symbol];

            var (highs, lows, closes) = GetLatestKlineArrays(symbol);
            if (closes.Count < AtrPeriod + 1)
            {
                Console.WriteLine($"[{symbol}] SKIP: Not enough Kline data to chase grid.");
                return;
            }

            decimal atr = CalculateAtr(AtrPeriod, highs, lows, closes);
            decimal spacing = atr * config.AtrMultipler;
            const decimal tolerance = 0.0008m;

            // If we’re already at MaxBotsPerSymbol overall, skip any placement
            int currentlyOpen = _activeOrders.Values.Count(o => o.Symbol == symbol);
            if (currentlyOpen >= config.MaxBotsPerSymbol)
                return;

            // 1) Add longs below price if needed
            while (longRungs.Count > 0 && livePrice < longRungs.First() - spacing)
            {
                if (_activeOrders.Values.Count(o => o.Symbol == symbol) >= config.MaxBotsPerSymbol)
                    break;

                decimal newPrice = longRungs.First() - spacing;
                if (_capitalManager.Available < maxCapitalPerBot)
                {
                    Console.WriteLine($"[{symbol}] SKIP: Not enough capital to add new long @ {newPrice:F4}");
                    break;
                }

                longRungs.Insert(0, newPrice);
                await PlaceSingleGridOrder(symbol, newPrice, "BUY", config, maxCapitalPerBot);
            }

            // 2) Add shorts above price if needed
            while (shortRungs.Count > 0 && livePrice > shortRungs.Last() + spacing)
            {
                if (_activeOrders.Values.Count(o => o.Symbol == symbol) >= config.MaxBotsPerSymbol)
                    break;

                decimal newPrice = shortRungs.Last() + spacing;
                if (_capitalManager.Available < maxCapitalPerBot)
                {
                    Console.WriteLine($"[{symbol}] SKIP: Not enough capital to add new short @ {newPrice:F4}");
                    break;
                }

                shortRungs.Add(newPrice);
                await PlaceSingleGridOrder(symbol, newPrice, "SELL", config, maxCapitalPerBot);
            }

            // 3) Cancel stale longs too far above price (thread-safe removal)
            while (true)
            {
                decimal removePrice;
                lock (longRungs)
                {
                    // re-check inside the lock
                    if (longRungs.Count == 0 || livePrice <= longRungs.First() + spacing * 2)
                        break;
                    removePrice = longRungs[0];
                    longRungs.RemoveAt(0);
                }
                // do the cancel outside the lock
                await CancelGridOrderByPrice(symbol, removePrice);
            }

            // 4) Cancel stale shorts too far below price (thread-safe removal)
            while (true)
            {
                decimal removePrice;
                lock (shortRungs)
                {
                    if (shortRungs.Count == 0 || livePrice >= shortRungs.Last() - spacing * 2)
                        break;
                    removePrice = shortRungs[shortRungs.Count - 1];
                    shortRungs.RemoveAt(shortRungs.Count - 1);
                }
                await CancelGridOrderByPrice(symbol, removePrice);
            }

            decimal lastPrice;
            lock (shortRungs)
            {
                if (shortRungs.Count == 0) return;
                lastPrice = shortRungs[shortRungs.Count - 1];
            }

            if (livePrice >= lastPrice - tolerance)
            {
                for (int i = 1; i <= 2; i++)
                {
                    decimal newShort = shortRungs.Last() + spacing * i;
                    bool existsShort = _activeOrders.Values.Any(o =>
                        o.Symbol == symbol &&
                        o.Side == "SELL" &&
                        Math.Abs(o.EntryPrice - newShort) <= tolerance);

                    if (!existsShort &&
                        _activeOrders.Values.Count(o => o.Symbol == symbol) < config.MaxBotsPerSymbol)
                    {
                        shortRungs.Add(newShort);
                        await PlaceSingleGridOrder(symbol, newShort, "SELL", config, maxCapitalPerBot);
                    }
                }
            }

            decimal firstPrice;
            lock (longRungs)
            {
                if (longRungs.Count == 0) return;
                firstPrice = longRungs[0];
            }

            if (livePrice <= firstPrice + tolerance)
            {
                for (int i = 1; i <= 2; i++)
                {
                    decimal newLong = longRungs.First() - spacing * i;
                    bool existsLong = _activeOrders.Values.Any(o =>
                        o.Symbol == symbol &&
                        o.Side == "BUY" &&
                        Math.Abs(o.EntryPrice - newLong) <= tolerance);

                    if (!existsLong &&
                        _activeOrders.Values.Count(o => o.Symbol == symbol) < config.MaxBotsPerSymbol)
                    {
                        longRungs.Insert(0, newLong);
                        await PlaceSingleGridOrder(symbol, newLong, "BUY", config, maxCapitalPerBot);
                    }
                }
            }

            // 6) Trend‐adjustment logic: flip/boost sides
            await AdjustForTrendAsync(symbol, config, livePrice, spacing, maxCapitalPerBot);

            _longRungs[symbol].Sort();
            _shortRungs[symbol].Reverse();
        }

        private async Task AdjustForTrendAsync(string symbol, GridConfig config, decimal currentPrice, decimal spacing, decimal maxCapitalPerBot)
        {

            int current = _activeOrders.Values.Count(o => o.Symbol == symbol);
            if (current >= config.MaxBotsPerSymbol)
            {
                Console.WriteLine($"[{symbol}] At max bot count ({current}). Skipping chase.");
                return;
            }

            const decimal thresholdPct = 0.60m;
            const int flipCount = 2;

            // live count guard
            bool AtCap() =>
                _activeOrders.Values.Count(o => o.Symbol == symbol)
                >= config.MaxBotsPerSymbol;

            // snapshot buys/sells
            var buys = _activeOrders.Values.Where(o => o.Symbol == symbol && o.Side == "BUY").ToList();
            var sells = _activeOrders.Values.Where(o => o.Symbol == symbol && o.Side == "SELL").ToList();

            // PnL helpers
            Func<GridOrder, decimal> buyPnl = o => (currentPrice - o.EntryPrice) * ((o.CapitalAllocated * o.Leverage) / o.EntryPrice);
            Func<GridOrder, decimal> sellPnl = o => (o.EntryPrice - currentPrice) * ((o.CapitalAllocated * o.Leverage) / o.EntryPrice);

            // 1) Flip the worst losing buys into sells
            int losingBuys = buys.Count(o => buyPnl(o) < 0);
            if (!AtCap() && losingBuys >= Math.Ceiling(config.LongOrderCount * thresholdPct))
            {
                foreach (var gb in buys.OrderBy(b => buyPnl(b)).Take(flipCount))
                {
                    if (AtCap()) break;
                    await _client.CancelOrdersAsync(symbol, new[] { gb.ClientId });
                    if (_activeOrders.TryRemove(gb.ClientId, out _))
                    {
                        _capitalManager.ReleaseMargin(gb.ClientId);
                        _longRungs[symbol].Remove(gb.EntryPrice);
                        Console.WriteLine($"[{symbol}] Flipping BUY {gb.ClientId} @ {gb.EntryPrice:F4}");
                    }
                    decimal sellPrice = currentPrice + spacing;
                    var newId = await PlaceSingleGridOrder(symbol, sellPrice, "SELL", config, maxCapitalPerBot);
                    if (newId != null) _shortRungs[symbol].Add(sellPrice);
                }
            }

            // 2) Flip the worst losing sells into buys
            int losingSells = sells.Count(o => sellPnl(o) < 0);
            if (!AtCap() && losingSells >= Math.Ceiling(config.ShortOrderCount * thresholdPct))
            {
                foreach (var gs in sells.OrderBy(s => sellPnl(s)).Take(flipCount))
                {
                    if (AtCap()) break;
                    await _client.CancelOrdersAsync(symbol, new[] { gs.ClientId });
                    if (_activeOrders.TryRemove(gs.ClientId, out _))
                    {
                        _capitalManager.ReleaseMargin(gs.ClientId);
                        _shortRungs[symbol].Remove(gs.EntryPrice);
                        Console.WriteLine($"[{symbol}] Flipping SELL {gs.ClientId} @ {gs.EntryPrice:F4}");
                    }
                    decimal buyPrice = currentPrice - spacing;
                    var newId = await PlaceSingleGridOrder(symbol, buyPrice, "BUY", config, maxCapitalPerBot);
                    if (newId != null) _longRungs[symbol].Insert(0, buyPrice);
                }
            }

            // 3) Reward the winning side by reallocating from the other
            int winningBuys = buys.Count(o => buyPnl(o) > 0);
            int winningSells = sells.Count(o => sellPnl(o) > 0);

            if (!AtCap() && winningBuys >= Math.Ceiling(config.LongOrderCount * thresholdPct) && sells.Any())
            {
                foreach (var ws in sells.OrderBy(s => sellPnl(s)).Take(flipCount))
                {
                    if (AtCap()) break;
                    await _client.CancelOrdersAsync(symbol, new[] { ws.ClientId });
                    if (_activeOrders.TryRemove(ws.ClientId, out _))
                    {
                        _capitalManager.ReleaseMargin(ws.ClientId);
                        _shortRungs[symbol].Remove(ws.EntryPrice);
                        Console.WriteLine($"[{symbol}] Realloc SELL→BUY {ws.ClientId}@{ws.EntryPrice:F4}");
                    }
                    decimal buyPrice = currentPrice - spacing;
                    var newId = await PlaceSingleGridOrder(symbol, buyPrice, "BUY", config, maxCapitalPerBot);
                    if (newId != null) _longRungs[symbol].Insert(0, buyPrice);
                }
            }

            if (!AtCap() && winningSells >= Math.Ceiling(config.ShortOrderCount * thresholdPct) && buys.Any())
            {
                foreach (var wb in buys.OrderBy(b => buyPnl(b)).Take(flipCount))
                {
                    if (AtCap()) break;
                    await _client.CancelOrdersAsync(symbol, new[] { wb.ClientId });
                    if (_activeOrders.TryRemove(wb.ClientId, out _))
                    {
                        _capitalManager.ReleaseMargin(wb.ClientId);
                        _longRungs[symbol].Remove(wb.EntryPrice);
                        Console.WriteLine($"[{symbol}] Realloc BUY→SELL {wb.ClientId}@{wb.EntryPrice:F4}");
                    }
                    decimal sellPrice = currentPrice + spacing;
                    var newId = await PlaceSingleGridOrder(symbol, sellPrice, "SELL", config, maxCapitalPerBot);
                    if (newId != null) _shortRungs[symbol].Add(sellPrice);
                }
            }

            // keep rung lists sorted
            _longRungs[symbol].Sort();
            _shortRungs[symbol].Sort();
        }



        public async Task ShiftGridUp(string symbol, int slideRungs, decimal spacing, decimal maxCapitalPerBot)
        {
            // 1) Cancel all open orders for this symbol
            var allOpen = _activeOrders.Where(kv => kv.Value.Symbol == symbol)
                                       .Select(kv => kv.Key)
                                       .ToList();

            await CancelAndReleaseAsync(symbol, allOpen);

            // 2) Shift rung lists up
            var longs = _longRungs[symbol];
            var shorts = _shortRungs[symbol];
            for (int i = 0; i < longs.Count; i++)
                longs[i] += slideRungs * spacing;
            for (int i = 0; i < shorts.Count; i++)
                shorts[i] += slideRungs * spacing;

            // 3) Place new orders on shifted rungs
            foreach (var price in longs.ToList())
            {
                await PlaceSingleGridOrder(symbol, price, "BUY", _configBySymbol[symbol], maxCapitalPerBot);
            }

            foreach (var price in shorts.ToList())
            {
                await PlaceSingleGridOrder(symbol, price, "SELL", _configBySymbol[symbol], maxCapitalPerBot);
            }

            Console.WriteLine($"[{symbol}] Shifted grid UP by {slideRungs} rungs (±{slideRungs * spacing:F4}).");
        }

        public async Task ShiftGridDown(string symbol, int slideRungs, decimal spacing, decimal maxCapitalPerBot)
        {
            // 1) Cancel all open orders for this symbol
            var allOpen = _activeOrders.Where(kv => kv.Value.Symbol == symbol)
                                       .Select(kv => kv.Key)
                                       .ToList();

            await CancelAndReleaseAsync(symbol, allOpen);

            // 2) Shift rung lists down
            var longs = _longRungs[symbol];
            var shorts = _shortRungs[symbol];
            for (int i = 0; i < longs.Count; i++)
                longs[i] -= slideRungs * spacing;
            for (int i = 0; i < shorts.Count; i++)
                shorts[i] -= slideRungs * spacing;

            // 3) Place new orders on shifted rungs
            foreach (var price in longs.ToList())
            {
                await PlaceSingleGridOrder(symbol, price, "BUY", _configBySymbol[symbol], maxCapitalPerBot);
            }

            foreach (var price in shorts.ToList())
            {
                await PlaceSingleGridOrder(symbol, price, "SELL", _configBySymbol[symbol], maxCapitalPerBot);
            }

            Console.WriteLine($"[{symbol}] Shifted grid DOWN by {slideRungs} rungs (±{slideRungs * spacing:F4}).");
        }

        public async Task CloseAllPositionsAsync(string symbol)
        {
            // 1) Release margin and remove from in-memory state
            var toRemove = _activeOrders.Keys
                .Where(orderId => _activeOrders[orderId].Symbol.Equals(symbol, StringComparison.OrdinalIgnoreCase))
                .ToList();

            foreach (var orderId in toRemove)
            {
                _capitalManager.ReleaseMargin(orderId);
                _activeOrders.TryRemove(orderId, out _);
            }

            // 2) Send the “close all” request for this symbol
            await _client.CloseAllPositionsAsync(symbol);
        }

        public async Task PreloadInitialKlinesAsync(string symbol)
        {
            _klineBuffers.TryAdd(symbol, new FixedSizeQueue<Candle>(AtrPeriod + 1));
            int needed = AtrPeriod + 1;

            var restResponse = await _client.GetKlineAsync(
                symbol: symbol,
                interval: "1m",
                limit: needed);

            var payload = restResponse?.Data?.Data;
            if (payload == null || payload.Count == 0)
            {
                Console.WriteLine($"[KLINE PRELOAD] Warning: No Kline entries returned for {symbol}.");
                return;
            }

            foreach (var entry in payload)
            {
                var candle = new Candle
                {
                    CloseTimeUtc = DateTimeOffset
                        .FromUnixTimeMilliseconds(entry.Time).UtcDateTime,
                    Open = entry.Open,
                    High = entry.High,
                    Low = entry.Low,
                    Close = entry.Close
                };

                _klineBuffers[symbol].Enqueue(candle);
            }

            Console.WriteLine($"[KLINE PRELOAD] Loaded {payload.Count} candles for {symbol}.");
        }

        private void DisplayGridLadder(string symbol, decimal centerPrice, GridConfig config)
        {
            var (hs, ls, cs) = GetLatestKlineArrays(symbol);
            decimal atr = CalculateAtr(AtrPeriod, hs, ls, cs);
            decimal spacing = atr * config.AtrMultipler;

            Console.WriteLine($"\n📊 Grid Ladder for {symbol} at center: {centerPrice:F2} (ATR={atr:F4}, spacing={spacing:F4})");

            for (int i = 1; i <= config.LongOrderCount; i++)
            {
                decimal price = centerPrice - spacing * i;
                decimal tp = price + (price * config.TakeProfitPct);
                decimal sl = price - (price * config.StopLossPct);
                Console.WriteLine($"  🟢 LONG  | Entry: {price:F2} | Qty: {config.OrderQuantity} | TP: {tp:F2} | SL: {sl:F2}");
            }

            for (int i = 1; i <= config.ShortOrderCount; i++)
            {
                decimal price = centerPrice + spacing * i;
                decimal tp = price - (price * config.TakeProfitPct);
                decimal sl = price + (price * config.StopLossPct);
                Console.WriteLine($"  🔴 SHORT | Entry: {price:F2} | Qty: {config.OrderQuantity} | TP: {tp:F2} | SL: {sl:F2}");
            }

            Console.WriteLine();
        }
    }

    public class GridConfig
    {
        public int LongOrderCount { get; set; } = 5;
        public int ShortOrderCount { get; set; } = 5;
        public decimal TakeProfitPct { get; set; } = 0.4m;
        public decimal StopLossPct { get; set; } = 0.4m;
        public decimal Leverage { get; set; } = 25m;
        public decimal OrderQuantity { get; set; } = 0.01m;
        public decimal AtrMultipler { get; set; } = 1m;
        public int StaleAgeSeconds { get; set; } = 20;
        public int MaxBotsPerSymbol { get; set; } = 10;

        // New config fields
        public bool EnableGroupedTakeProfit { get; set; } = true;
        public decimal GroupTakeProfitPct { get; set; } = 0.6m; // 0.6% grouped gain
        public bool UseStopLoss { get; set; } = true;

        public decimal MaxLossPerSideUsd { get; set; } = 10m;
        public decimal MaxLossPerSidePct { get; set; } = 0.02m;
    }

    public class GridOrder
    {
        public string ClientId { get; set; }
        public string Symbol { get; set; }
        public decimal CapitalAllocated { get; set; }
        public DateTime CreateAt { get; set; }
        public decimal EntryPrice { get; set; }
        public string Side { get; }
        public decimal Leverage { get; }

        public GridOrder(string clientId, decimal margin, decimal entryPrice, string symbol, decimal leverage, string side)
        {
            ClientId = clientId;
            CapitalAllocated = margin;
            CreateAt = DateTime.UtcNow;
            EntryPrice = entryPrice;
            Symbol = symbol;
            Leverage = leverage;
            Side = side;
        }



        public bool IsStale(int ageSeconds) =>
            (DateTime.UtcNow - CreateAt).TotalSeconds > ageSeconds;
    }

    public class LiveCapitalManager
    {
        private readonly object _lock = new();
        private decimal _totalCapital;
        private decimal _allocatedCapital;
        private readonly Dictionary<string, decimal> _orderMargins = new();

        public LiveCapitalManager(decimal totalCapital)
        {
            _totalCapital = totalCapital;
            _allocatedCapital = 0m;
        }

        public void ReserveMargin(string orderId, decimal margin)
        {
            lock (_lock)
            {
                if (_orderMargins.ContainsKey(orderId))
                    return;

                if (_allocatedCapital + margin > _totalCapital)
                    throw new InvalidOperationException(
                        $"Cannot reserve ${margin:F4} (order {orderId}) because only ${_totalCapital - _allocatedCapital:F4} is free.");

                _orderMargins[orderId] = margin;
                _allocatedCapital += margin;
                Console.WriteLine($"[CAPITAL] Reserved ${margin:F4} for {orderId}. " +
                                  $"Allocated={_allocatedCapital:F4}, Available={_totalCapital - _allocatedCapital:F4}");
            }
        }

        public int ActiveOrderCount
        {
            get { lock (_lock) { return _orderMargins.Count; } }
        }

        public void ReleaseMargin(string orderId)
        {
            lock (_lock)
            {
                if (!_orderMargins.TryGetValue(orderId, out var margin))
                    return;

                _orderMargins.Remove(orderId);
                _allocatedCapital -= margin;
                if (_allocatedCapital < 0m) _allocatedCapital = 0m;

                Console.WriteLine($"[CAPITAL] Released ${margin:F4} for {orderId}. " +
                                  $"Allocated={_allocatedCapital:F4}, Available={_totalCapital - _allocatedCapital:F4}");
            }
        }

        public decimal Available
        {
            get { lock (_lock) { return _totalCapital - _allocatedCapital; } }
        }

        public decimal Allocated
        {
            get { lock (_lock) { return _allocatedCapital; } }
        }

        public void RefreshTotalCapital(decimal avail)
        {
            lock (_lock)
            {
                _totalCapital = avail + _allocatedCapital;
                Console.WriteLine($"[CAPITAL] Refreshed total capital to ${_totalCapital:F4} – Allocated still ${_allocatedCapital:F4}, Available now ${_totalCapital - _allocatedCapital:F4}");
            }
        }
    }
}
