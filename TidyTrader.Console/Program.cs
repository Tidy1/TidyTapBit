// File: Program.cs
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

using TidyTrader.ApiIntegration.LadderStrategy;
using TidyTrader.ApiIntegration.Models;
using TidyTrader.ApiIntegration.Models.Responses.Trade;

class Program
{
    static async Task Main(string[] args)
    {
        // ── CONFIG & SETUP ──
        var apiKey = "653a087e87950bebb7607d0db19c0934";
        var apiSecret = "6ab398029715595bbd5959eeab8b2846";
        // 1) define your per-symbol decimal precision:
        var symbols = new List<string> { "BTCUSDT", "HBARUSDT", "1000PEPEUSDT", "WIFUSDT" };
        var symbolPrecision = new Dictionary<string, int>
        {
            ["BTCUSDT"] = 2,  // 0.01 ticks
            ["HBARUSDT"] = 5,  // 0.00001 ticks
            ["1000PEPEUSDT"] = 6,  // 0.000001 ticks
            ["WIFUSDT"] = 4,  // 0.0001 ticks
        };
        decimal totalCap = 219.48m;

        Console.WriteLine("=== Ladder Bot Console ===");
        Console.WriteLine($"Total Capital: ${totalCap}\n");

        var apiClient = new BitunixApiClient(apiKey, apiSecret);
        var capitalManager = new LiveCapitalManager(totalCap);
        var adapter = new OrderServiceAdapter(apiClient, capitalManager, 5m);

        // track per-symbol init
        var initialized = new HashSet<string>();

        var wsPublic = new BitunixWebSocketClient(apiKey, apiSecret, "wss://fapi.bitunix.com/public/");
        var wsPrivate = new BitunixWebSocketClient(apiKey, apiSecret, "wss://fapi.bitunix.com/private/");
        await wsPublic.ConnectAsync();
        await wsPrivate.ConnectAsync();

        // subscribe streams
        foreach (var s in symbols) await wsPublic.SubscribeToPriceAsync(s);
        await wsPrivate.SubscribeToBalanceAsync();
        await wsPrivate.SubscribeToOrderAsync();

        // build one ladder per symbol
        var ladders = new Dictionary<string, SmartRecenterLadder>();
        foreach (var sym in symbols)
        {
            var gridCfg = new GridConfig
            {
                LongOrderCount = 5,
                ShortOrderCount = 5,
                TakeProfitPct = 0.1m,
                StopLossPct = 2.5m,
                AtrMultipler = 1.2m,
                EnableGroupedTakeProfit = true,
                GroupTakeProfitPct = 0.006m,
                UseStopLoss = true,
                MaxBotsPerSymbol = 10
            };
            var ladderCfg = new LadderConfig
            {
                BaseRungsPerSide = gridCfg.LongOrderCount,
                SpacingMultiplier = gridCfg.AtrMultipler,
                TakeProfitPct = gridCfg.TakeProfitPct / 100m,
                StopLossPct = gridCfg.StopLossPct / 100m,
                FundingRateAdjustment = 0m,
                RungsToTpRecenter = 2
            };

            // async ATR→spacing helper
            async Task<decimal> ComputeSpacingAsync()
            {
                var resp = await apiClient.GetKlineAsync(sym, "1m", limit: 15);
                var klines = resp.Data.Data;
                var highs = klines.Select(k => k.High).ToList();
                var lows = klines.Select(k => k.Low).ToList();
                var closes = klines.Select(k => k.Close).ToList();
                var atr = CalculateAtr(14, highs, lows, closes);
                return atr * ladderCfg.SpacingMultiplier;
            }

            // capture precision for this symbol
            int prec = symbolPrecision[sym];
            decimal multiplier = (decimal)Math.Pow(10, prec);

            // quantize: floor to tick
            Func<decimal, decimal> quantize = raw =>
                Math.Floor(raw * multiplier) / multiplier;


            var ladder = new SmartRecenterLadder(
            sym,
            ladderCfg,
            adapter,
            ComputeSpacingAsync,
            quantize      // ← pass your per-symbol quantizer here
             );

            ladders[sym] = ladder;

            ladder.OnRungsUpdated += rungSet =>
            {
                Console.WriteLine($"\n[{sym}] Ladder re-centered @ {DateTime.UtcNow:T}");
                foreach (var r in rungSet)
                    Console.WriteLine($"  {r.Side} @ {r.Price:F4} → OrderId={r.OrderId}");
                Console.WriteLine();
            };
        }

        // order fills → ladder
        wsPrivate.OnOrderUpdate += async updates =>
        {
            foreach (var o in updates)
            {
                if (o.Event != "FILLED") continue;
                if (!ladders.TryGetValue(o.Symbol, out var ladder)) continue;

                var side = o.Side == "BUY" ? OrderSide.Long : OrderSide.Short;
                var fillPrice = decimal.Parse(o.Price, CultureInfo.InvariantCulture);

                adapter.NotifyFill(o.OrderId, fillPrice);
                await ladder.OnOrderFilledAsync(o.OrderId, side, fillPrice);

                //Console.WriteLine("[DEBUG] OnOrderUpdate Hit");
            }
        };

        // price ticks → initialize once per symbol, then feed the ladder
        wsPublic.OnPriceUpdate += async update =>
        {
            if (!ladders.TryGetValue(update.Symbol, out var ladder)) return;
            if (!decimal.TryParse(update.Data.MarketPrice,
                NumberStyles.Any, CultureInfo.InvariantCulture, out var price)) return;

            if (!initialized.Contains(update.Symbol))
            {
                initialized.Add(update.Symbol);
                Console.WriteLine($"[{update.Symbol}] INITIALIZING ladder @ {price:F4}");
                await ladder.InitializeAsync(price);
            }
            else
            {
                await ladder.OnPriceTickAsync(price);
            }
        };

        // keep capital manager in sync
        wsPrivate.OnBalanceUpdate += balances =>
        {
            var usdt = balances.FirstOrDefault(b =>
                b.Coin.Equals("USDT", StringComparison.OrdinalIgnoreCase));
            if (usdt != null &&
                decimal.TryParse(usdt.Available,
                                NumberStyles.Any, CultureInfo.InvariantCulture, out var avail))
            {
                capitalManager.RefreshTotalCapital(avail);
            }
        };

        TimeSpan reportInterval = TimeSpan.FromMinutes(1);

        var reportTimer = new System.Threading.Timer(_ =>
        {
            Console.WriteLine($"[{DateTime.UtcNow:T}] ⏱ SUMMARY REPORT — Active Orders: {capitalManager.ActiveOrderCount}, Allocated: ${capitalManager.Allocated:F2}, Available: ${capitalManager.Available:F2}");
        }, null, reportInterval, reportInterval);

        // run forever
        await Task.Delay(-1);
    }

    // ── LOCAL ATR CALCULATOR ──
    static decimal CalculateAtr(int period, List<decimal> highs, List<decimal> lows, List<decimal> closes)
    {
        var trs = new List<decimal>();
        for (int i = 1; i < highs.Count; i++)
        {
            var tr1 = highs[i] - lows[i];
            var tr2 = Math.Abs(highs[i] - closes[i - 1]);
            var tr3 = Math.Abs(lows[i] - closes[i - 1]);
            trs.Add(Math.Max(tr1, Math.Max(tr2, tr3)));
        }

        if (trs.Count < period)
            return trs.Count == 0 ? 0m : trs.Average();

        return trs.Skip(trs.Count - period).Take(period).Average();
    }
}

