using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace TidyTrader.ApiIntegration.Models
{
    public class BitunixWebSocketClient : IDisposable
    {
        private readonly string _apiKey;
        private readonly string _apiSecret;
        private readonly string _baseUrl;
        private ClientWebSocket _webSocket;
        private CancellationTokenSource _cts;
        private CancellationTokenSource _pingCts;

        // We will complete this TaskCompletionSource once we see a successful "login" response.
        private TaskCompletionSource<bool> _loginTcs;

        // Keep track of which private channels we’ve asked to subscribe so that on reconnect
        // we can re‐subscribe automatically.
        private readonly List<Func<Task>> _pendingPrivateSubscriptions = new();

        public WebSocketState WebSocketState => _webSocket?.State ?? WebSocketState.None;

        public event Action<long> OnPong;
        public event Action<List<BitunixBalanceData>> OnBalanceUpdate;
        public event Action<List<BitunixOrderData>> OnOrderUpdate;
        public event Action<List<BitunixPositionData>> OnPositionUpdate;
        public event Action<DepthBook> OnDepthUpdate;
        public event Action<PriceUpdate> OnPriceUpdate;
        public event Action<BitunixTickerData> OnTickerUpdate;
        public event Action<List<BitunixTickerItem>> OnTickersUpdate;
        public event Action<List<BitunixTradeData>> OnTradeUpdate;
        public event Action<decimal> OnFundingUpdate;
        public event Action<string> OnKlineUpdate;
        public event Action OnDisconnected;
        public event Action OnOpen;
        public event Action<string> OnRawMessage;

        // If you want to expose “login succeeded” as an event:
        public event Action OnLoginSuccess;


        private readonly Dictionary<string, decimal> _latestMarketPrices = new();

        // We keep a list of symbols we subscribed on the PUBLIC side, so we can re‐subscribe after reconnect.
        private readonly List<string> _publicKlineSubscriptions = new();
        private readonly List<string> _publicPriceSubscriptions = new();

        public BitunixWebSocketClient(string apiKey, string apiSecret, string baseUrl)
        {
            _apiKey = apiKey;
            _apiSecret = apiSecret;
            _baseUrl = baseUrl.TrimEnd('/'); // e.g. "wss://fapi.bitunix.com/private"
        }

        private string GenerateSignature(long timestamp, string nonce)
        {
            using var sha256 = SHA256.Create();
            // first: SHA256(nonce + timestamp + apiKey)
            var hash1 = sha256.ComputeHash(Encoding.UTF8.GetBytes(nonce + timestamp + _apiKey));
            var hex1 = BitConverter.ToString(hash1).Replace("-", "").ToLower();
            // then: SHA256(firstHex + secretKey)
            var hash2 = sha256.ComputeHash(Encoding.UTF8.GetBytes(hex1 + _apiSecret));
            return BitConverter.ToString(hash2).Replace("-", "").ToLower();
        }

        /// <summary>
        /// ConnectAsync will:
        /// 1) Open the WebSocket and wait for the handshake to complete.
        /// 2) Send the "login" frame immediately after ConnectAsync returns (because ConnectAsync on ClientWebSocket
        ///    only returns once the WS handshake is done).
        /// 3) Start ReceiveLoop and a keep‐alive ping loop.
        /// 4) Wait for a successful "login" acknowledgment (or throw if none arrives in 10s).
        /// </summary>
        public async Task ConnectAsync()
        {
            _webSocket = new ClientWebSocket();
            _webSocket.Options.KeepAliveInterval = TimeSpan.FromSeconds(20);

            _cts = new CancellationTokenSource();

            Console.WriteLine($"[WS] Connecting to {_baseUrl}");
            await _webSocket.ConnectAsync(new Uri(_baseUrl), CancellationToken.None);
            Console.WriteLine("[WS] Connected (handshake complete)");

            // Fire OnOpen for any listeners
            OnOpen?.Invoke();

            // Reset the login TCS so we can await it
            _loginTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            // Immediately send our login payload
            await AuthenticateAsync();

            // start receive loop
            _ = ReceiveLoop(_cts.Token);

            // start JSON‐ping keepalive
            _pingCts = new CancellationTokenSource();
            _ = Task.Run(async () =>
            {
                var token = _pingCts.Token;
                while (!token.IsCancellationRequested && _webSocket.State == WebSocketState.Open)
                {
                    try { await SendPingAsync(); }
                    catch { /* let ReceiveLoop handle disconnects */ }
                    await Task.Delay(TimeSpan.FromSeconds(10), token);
                }
            }, _pingCts.Token);

            // wait for login ack…
            var loginTimeout = Task.Delay(TimeSpan.FromSeconds(20));
            var completed = await Task.WhenAny(_loginTcs.Task, loginTimeout);
            OnLoginSuccess?.Invoke();
            foreach (var subscribe in _pendingPrivateSubscriptions.ToList())
                await subscribe();
        }

        private async Task AuthenticateAsync()
        {
            var ts = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var nonce = Guid.NewGuid().ToString("N").Substring(0, 32);
            var sign = GenerateSignature(ts, nonce);

            var login = new
            {
                op = "login",
                args = new[]
                {
                    new
                    {
                        apiKey = _apiKey,
                        timestamp = ts,
                        nonce,
                        sign
                    }
                }
            };
            var msg = JsonConvert.SerializeObject(login);
            Console.WriteLine($"[WS TX] {msg}");
            var buf = Encoding.UTF8.GetBytes(msg);
            await _webSocket.SendAsync(buf, WebSocketMessageType.Text, true, CancellationToken.None);
        }

        public decimal? GetLatestPrice(string symbol)
        {
            return _latestMarketPrices.TryGetValue(symbol, out var price)
                ? price
                : (decimal?)null;
        }

        /// <summary>
        /// SubscribeToBalanceAsync, SubscribeToOrderAsync, SubscribeToPositionAsync all queue up a subscription
        /// function so that on reconnect we can just re‐invoke them.  We also Invoke them immediately if we are
        /// already logged in.
        /// </summary>
        public async Task SubscribeToBalanceAsync()
        {
            // We store the delegate, so on reconnect, we can do it again:
            _pendingPrivateSubscriptions.Add(SubscribeToBalanceAsync);

            // Now if we’ve already passed the login stage (i.e. _loginTcs is completed), send it now:
            if (_loginTcs?.Task.IsCompleted == true && _loginTcs.Task.Result)
            {
                await SendBalanceSubscribeFrame();
            }
        }

        public async Task SubscribeToFundingRateAsync(string symbol)
        {
            _pendingPrivateSubscriptions.Add(() => SubscribeToFundingRateAsync(symbol));

            if (_loginTcs?.Task.IsCompleted == true && _loginTcs.Task.Result)
            {
                // the exact payload will depend on the API spec; 
                // replace "fundingRate" with the channel name the docs specify:
                var frame = new
                {
                    op = "subscribe",
                    args = new[] { new { channel = "fundingRate", symbol = symbol } }
                };

                var msg = JsonConvert.SerializeObject(frame);
                Console.WriteLine($"[WS TX] {msg}");
                var buf = Encoding.UTF8.GetBytes(msg);
                await _webSocket.SendAsync(buf, WebSocketMessageType.Text, true, CancellationToken.None);
            }
        }


        public async Task SubscribeToOrderAsync(string symbol = null)
        {
            _pendingPrivateSubscriptions.Add(async () => await SubscribeToOrderAsync(symbol));
            if (_loginTcs?.Task.IsCompleted == true && _loginTcs.Task.Result)
            {
                await SendOrderSubscribeFrame();
            }
        }

        public async Task SubscribeToPositionAsync()
        {
            _pendingPrivateSubscriptions.Add(SubscribeToPositionAsync);
            if (_loginTcs?.Task.IsCompleted == true && _loginTcs.Task.Result)
            {
                await SendPositionSubscribeFrame();
            }
        }

        private async Task SendBalanceSubscribeFrame()
        {
            var ts = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var nonce = Guid.NewGuid().ToString("N").Substring(0, 32);
            var sign = GenerateSignature(ts, nonce);

            var sub = new
            {
                op = "subscribe",
                args = new[]
                {
                    new
                    {
                        ch = "position",    // balance / position both come over "ch":"position"
                        apiKey = _apiKey,
                        timestamp = ts,
                        nonce,
                        sign
                    }
                }
            };
            var msg = JsonConvert.SerializeObject(sub);
            Console.WriteLine($"[WS TX Balance] {msg}");
            var buf = Encoding.UTF8.GetBytes(msg);
            await _webSocket.SendAsync(buf, WebSocketMessageType.Text, true, CancellationToken.None);
        }

        private async Task SendOrderSubscribeFrame()
        {
            var ts = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var nonce = Guid.NewGuid().ToString("N").Substring(0, 32);
            var sign = GenerateSignature(ts, nonce);

            var sub = new
            {
                op = "subscribe",
                args = new[]
                {
                    new
                    {
                        ch = "order",
                        apiKey = _apiKey,
                        timestamp = ts,
                        nonce,
                        sign
                    }
                }
            };
            var msg = JsonConvert.SerializeObject(sub);
            Console.WriteLine($"[WS TX Order] {msg}");
            var buf = Encoding.UTF8.GetBytes(msg);
            await _webSocket.SendAsync(buf, WebSocketMessageType.Text, true, CancellationToken.None);
        }

        private async Task SendPositionSubscribeFrame()
        {
            var ts = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var nonce = Guid.NewGuid().ToString("N").Substring(0, 32);
            var sign = GenerateSignature(ts, nonce);

            var sub = new
            {
                op = "subscribe",
                args = new[]
                {
                    new
                    {
                        ch = "position",
                        apiKey = _apiKey,
                        timestamp = ts,
                        nonce,
                        sign
                    }
                }
            };
            var msg = JsonConvert.SerializeObject(sub);
            Console.WriteLine($"[WS TX Position] {msg}");
            var buf = Encoding.UTF8.GetBytes(msg);
            await _webSocket.SendAsync(buf, WebSocketMessageType.Text, true, CancellationToken.None);
        }

        /// <summary>
        /// Public‐side subscriptions (price, kline, etc.) also get saved so that on reconnect
        /// we can re-subscribe them.  We keep distinct lists for klines vs price.
        /// </summary>
        public async Task SubscribeToDepthAsync(string symbol, string channel)
        {
            var sub = new
            {
                op = "subscribe",
                args = new[] { new { symbol, ch = channel } }
            };
            var msg = JsonConvert.SerializeObject(sub);
            Console.WriteLine($"[WS TX] {msg}");
            var buffer = Encoding.UTF8.GetBytes(msg);
            await _webSocket.SendAsync(buffer, WebSocketMessageType.Text, true, CancellationToken.None);
        }

        public async Task SubscribeToPriceAsync(string symbol)
        {
            // Save for re‐subscribe on reconnect
            _publicPriceSubscriptions.Add(symbol);

            var sub = new
            {
                op = "subscribe",
                args = new[] { new { symbol = symbol, ch = "price" } }
            };
            var msg = JsonConvert.SerializeObject(sub);
            var buf = Encoding.UTF8.GetBytes(msg);
            await _webSocket.SendAsync(buf, WebSocketMessageType.Text, true, CancellationToken.None);
        }

        public async Task SubscribeToTickerAsync(string symbol)
        {
            var sub = new
            {
                op = "subscribe",
                args = new[] { new { symbol, ch = "ticker" } }
            };
            var msg = JsonConvert.SerializeObject(sub);
            Console.WriteLine($"[WS TX] {msg}");
            await _webSocket.SendAsync(
                Encoding.UTF8.GetBytes(msg),
                WebSocketMessageType.Text,
                true,
                CancellationToken.None
            );
        }

        public async Task SubscribeToAggregatedTickersAsync(params string[] symbols)
        {
            var args = symbols
                .Select(sym => new { symbol = sym, ch = "tickers" })
                .ToArray();
            var msg = new { op = "subscribe", args };
            var json = JsonConvert.SerializeObject(msg);
            Console.WriteLine($"[WS TX] {json}");
            await _webSocket.SendAsync(
                Encoding.UTF8.GetBytes(json),
                WebSocketMessageType.Text, true, CancellationToken.None
            );
        }

        public async Task SubscribeToTradeAsync(string symbol)
        {
            var msg = new
            {
                op = "subscribe",
                args = new[] { new { symbol = symbol, ch = "trade" } }
            };
            var json = JsonConvert.SerializeObject(msg);
            await _webSocket.SendAsync(
                Encoding.UTF8.GetBytes(json),
                WebSocketMessageType.Text,
                true,
                CancellationToken.None
            );
        }

        public async Task SubscribeToKlineAsync(string symbol, string interval = "1m")
        {
            // Save for re‐subscribe
            _publicKlineSubscriptions.Add(symbol);

            var sub = new
            {
                op = "subscribe",
                args = new[] { new { symbol = symbol, ch = $"market_kline_{interval}" } }
            };
            var msg = JsonConvert.SerializeObject(sub);
            var buf = Encoding.UTF8.GetBytes(msg);
            await _webSocket.SendAsync(buf, WebSocketMessageType.Text, true, CancellationToken.None);
        }

        public async Task SendPingAsync()
        {
            var ping = new { op = "ping", ping = DateTimeOffset.UtcNow.ToUnixTimeSeconds() };
            var msg = JsonConvert.SerializeObject(ping);
            var buf = Encoding.UTF8.GetBytes(msg);
            await _webSocket.SendAsync(buf, WebSocketMessageType.Text, true, CancellationToken.None);
        }

        public async Task CloseAsync()
        {
            if (_webSocket?.State == WebSocketState.Open)
            {
                Console.WriteLine("[WS] Sending close frame");
                await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Client closing", CancellationToken.None);
            }

            _cts?.Cancel();
            _pingCts?.Cancel();

            OnDisconnected?.Invoke();
            Console.WriteLine("[WS] Closed");
        }

        private async Task ReceiveLoop(CancellationToken ct)
        {
            var buffer = new byte[8 * 1024];

            while (!ct.IsCancellationRequested)
            {
                try
                {
                    var result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), ct);

                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        Console.WriteLine("[WS] Server requested close. Attempting reconnect...");
                        await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Ack close", CancellationToken.None);
                        throw new WebSocketException("Server closed connection");
                    }

                    var text = Encoding.UTF8.GetString(buffer, 0, result.Count);

                    OnRawMessage?.Invoke(text); // Fire raw message event
                    HandleMessage(text);
                }
                catch (WebSocketException wsex)
                {
                    Console.WriteLine($"[WS ERR] {wsex.Message}. Reconnecting in 2s...");

                    OnDisconnected?.Invoke();

                    try { _webSocket?.Abort(); } catch { }
                    _webSocket?.Dispose();

                    await Task.Delay(2000, ct);
                    if (ct.IsCancellationRequested) break;

                    // Reconnect and re‐login + re‐subscribe
                    await ReconnectAsync();
                }
                catch (OperationCanceledException)
                {
                    break; // Graceful shutdown
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[WS ERR] Unexpected: {ex.Message}\n{ex.StackTrace}");
                    await Task.Delay(2000);
                }
            }
        }

        private async Task ReconnectAsync()
        {
            try
            {
                OnDisconnected?.Invoke();

                _pingCts?.Cancel();
                _pingCts?.Dispose();

                try { _webSocket?.Abort(); } catch { }
                _webSocket?.Dispose();

                await Task.Delay(2000);

                _webSocket = new ClientWebSocket();
                _webSocket.Options.KeepAliveInterval = TimeSpan.FromSeconds(20);

                Console.WriteLine($"[WS] Reconnecting to {_baseUrl}");
                await _webSocket.ConnectAsync(new Uri(_baseUrl), CancellationToken.None);
                Console.WriteLine("[WS] Reconnected");

                // Fire OnOpen again
                OnOpen?.Invoke();

                // Reset login TCS
                _loginTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

                // Re‐send login
                await AuthenticateAsync();

                // Start a fresh ReceiveLoop
                _ = ReceiveLoop(_cts.Token);

                // Wait for login ack (again)
                var loginTimeout = Task.Delay(TimeSpan.FromSeconds(10));
                var completed = await Task.WhenAny(_loginTcs.Task, loginTimeout);
                if (completed != _loginTcs.Task || !_loginTcs.Task.Result)
                {
                    throw new Exception("WebSocket re‐login was not acknowledged in time.");
                }
                OnLoginSuccess?.Invoke();

                // Re-subscribe to public channels:
                foreach (var sym in _publicPriceSubscriptions)
                    await SubscribeToPriceAsync(sym);
                foreach (var sym in _publicKlineSubscriptions)
                    await SubscribeToKlineAsync(sym, "1m");

                // Re-subscribe to private channels (order, balance, position):
                foreach (var subscribeFunc in _pendingPrivateSubscriptions)
                {
                    await subscribeFunc();
                }

                // Restart ping loop
                _pingCts = new CancellationTokenSource();
                _ = Task.Run(async () =>
                {
                    var token = _pingCts.Token;
                    while (!token.IsCancellationRequested && _webSocket.State == WebSocketState.Open)
                    {
                        try
                        {
                            await SendPingAsync();
                        }
                        catch { }
                        await Task.Delay(TimeSpan.FromSeconds(30), token);
                    }
                }, _pingCts.Token);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[WS ERR] Reconnect failed: {ex.Message}. Retrying in 5s...");
                await Task.Delay(5000);
                await ReconnectAsync();
            }
        }

        private async Task SendPongAsync(long serverTs)
        {
            // The server sent you a {"op":"ping","ping":…,"pong":…}  
            // We’ll reply with op="pong" and echo back their ping timestamp
            var frame = new
            {
                op = "pong",
                pong = serverTs
            };
            var msg = JsonConvert.SerializeObject(frame);
            //Console.WriteLine($"[WS TX pong] {msg}");
            var buf = Encoding.UTF8.GetBytes(msg);
            await _webSocket.SendAsync(new ArraySegment<byte>(buf), WebSocketMessageType.Text, true, CancellationToken.None);
        }

        private async Task HandleMessage(string message)
        {
            try
            {
                //Console.WriteLine($"[RAW WS→] {message}");
                var j = JObject.Parse(message);

                // 1) If server is sending back {"op":"pong", "pong": <ts>}, treat it as a pong
                if (j["op"]?.ToString() == "pong")
                {
                    var ts = j["pong"]?.ToObject<long>() ?? 0;
                    OnPong?.Invoke(ts);
                    return;
                }

                if (j["op"]?.ToString() == "ping")
                {
                    // echo back a pong (or just call OnPong)
                    var pingVal = j["ping"]?.ToObject<long>() ?? 0;
                    await SendPongAsync(pingVal);

                    return;
                }

                // 2) If server is sending {"op":"login", "success":true}, mark login TCS as completed
                if (j["op"]?.ToString() == "login" && j["success"]?.Value<bool>() == true)
                {
                    _loginTcs?.TrySetResult(true);
                    return;
                }

                // 3) If server is telling us we failed to login
                if (j["op"]?.ToString() == "login" && j["success"]?.Value<bool>() == false)
                {
                    _loginTcs?.TrySetResult(false);
                    return;
                }

                // 4) Dispatch on "ch" channel
                var ch = j["ch"]?.ToString();
                switch (ch)
                {
                    case "position":
                        // "balance update" vs "position update" both come over ch="position"
                        var arr = j["data"] as JArray;
                        if (arr?.Count > 0)
                        {
                            var first = arr[0] as JObject;
                            if (first.ContainsKey("coin"))
                            {
                                // balance update
                                var balances = arr.ToObject<List<BitunixBalanceData>>();
                                OnBalanceUpdate?.Invoke(balances);
                            }
                            else
                            {
                                // position update
                                var positions = arr.ToObject<List<BitunixPositionData>>();
                                OnPositionUpdate?.Invoke(positions);
                            }
                        }
                        break;

                    case "order":
                        var single = j["data"]?.ToObject<BitunixOrderData>();
                        if (single != null)
                        {
                            OnOrderUpdate?.Invoke(new List<BitunixOrderData> { single });
                        }
                        break;

                    case "depth_books":
                    case "depth_book1":
                    case "depth_book5":
                    case "depth_book15":
                        var book = j.ToObject<DepthBook>();
                        OnDepthUpdate?.Invoke(book);
                        break;

                    case "funding_rate":
                        var arra = j["data"] as JArray;
                        if (arra?.Count > 0)
                        {
                            var el = arra[0];
                            if (el["fundingRateLast"] != null &&
                                decimal.TryParse(el["fundingRateLast"].ToString(),
                                                 NumberStyles.Any,
                                                 CultureInfo.InvariantCulture,
                                                 out var rate))
                            {
                                OnFundingUpdate?.Invoke(rate);
                            }
                        }
                        break;

                    case "price":
                        var pu = j.ToObject<PriceUpdate>();
                        if (pu != null && decimal.TryParse(pu.Data.MarketPrice, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsedPrice))
                        {
                            // 1) Update our internal cache:
                            _latestMarketPrices[pu.Symbol] = parsedPrice;

                            // 2) Fire the OnPriceUpdate event so subscribers (your GridOrderManager) can react:
                            OnPriceUpdate?.Invoke(pu);

                            // 3) Also fire OnFundingUpdate if they included a funding rate in this message:
                            OnFundingUpdate?.Invoke(
                                pu.Data.FundingRate != null
                                    ? decimal.Parse(pu.Data.FundingRate, CultureInfo.InvariantCulture)
                                    : 0m
                            );
                        }

                        // In case a separate FundingUpdate object is sent on the same "price" channel:
                        var fu = j.ToObject<FundingUpdate>();
                        if (fu != null && fu.Data != null)
                        {
                            OnFundingUpdate?.Invoke(
                                fu.Data.fundingRateLast != null
                                    ? decimal.Parse(fu.Data.fundingRateLast, CultureInfo.InvariantCulture)
                                    : 0m
                            );
                        }
                        break;

                    case "ticker":
                        var ticker = j["data"]?.ToObject<BitunixTickerData>();
                        if (ticker != null)
                            OnTickerUpdate?.Invoke(ticker);
                        break;

                    case "tickers":
                        var list = j["data"]?.ToObject<List<BitunixTickerItem>>();
                        if (list != null)
                            OnTickersUpdate?.Invoke(list);
                        break;

                    case "trade":
                        var trades = j["data"]?.ToObject<List<BitunixTradeData>>();
                        if (trades != null)
                            OnTradeUpdate?.Invoke(trades);
                        break;

                    case "kline_1m":
                        var payload = JObject.Parse(message);
                        var k = payload["data"]?["k"];
                        if (k != null && k.Value<bool>("x"))
                        {
                            OnKlineUpdate?.Invoke(message);
                        }
                        break;

                        // (Handle other intervals if desired; omitted for brevity)
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[WS ERR] {ex.Message}\nRaw: {message}");
            }
        }

        public void Dispose()
        {
            _cts?.Cancel();
            _pingCts?.Cancel();
            _webSocket?.Dispose();
        }


    }

    public class BitunixBalanceData
    {
        public string Coin { get; set; }
        public string Available { get; set; }
        public string Frozen { get; set; }
        public string IsolationFrozen { get; set; }
        public string CrossFrozen { get; set; }
        public string Margin { get; set; }
        public string IsolationMargin { get; set; }
        public string CrossMargin { get; set; }
        public string ExpMoney { get; set; }
    }

    public class BitunixOrderData
    {
        [JsonProperty("event")]
        public string Event { get; set; }

        [JsonProperty("orderId")]
        public string OrderId { get; set; }

        [JsonProperty("symbol")]
        public string Symbol { get; set; }

        [JsonProperty("positionType")]
        public string PositionType { get; set; }

        [JsonProperty("positionMode")]
        public string PositionMode { get; set; }

        [JsonProperty("side")]
        public string Side { get; set; }

        [JsonProperty("effect")]
        public string Effect { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("qty")]
        public string Qty { get; set; }

        [JsonProperty("reductionOnly")]
        public bool ReductionOnly { get; set; }

        [JsonProperty("price")]
        public string Price { get; set; }

        [JsonProperty("ctime")]
        public string Ctime { get; set; }

        [JsonProperty("mtime")]
        public string Mtime { get; set; }

        [JsonProperty("leverage")]
        public string Leverage { get; set; }

        [JsonProperty("orderStatus")]
        public string OrderStatus { get; set; }

        [JsonProperty("fee")]
        public string Fee { get; set; }

        [JsonProperty("tpStopType")]
        public string TpStopType { get; set; }

        [JsonProperty("tpPrice")]
        public string TpPrice { get; set; }

        [JsonProperty("tpOrderType")]
        public string TpOrderType { get; set; }

        [JsonProperty("tpOrderPrice")]
        public string TpOrderPrice { get; set; }

        [JsonProperty("slStopType")]
        public string SlStopType { get; set; }

        [JsonProperty("slPrice")]
        public string SlPrice { get; set; }

        [JsonProperty("slOrderType")]
        public string SlOrderType { get; set; }

        [JsonProperty("slOrderPrice")]
        public string SlOrderPrice { get; set; }
    }

    public class BitunixPositionData
    {
        [JsonProperty("event")]
        public string Event { get; set; }

        [JsonProperty("positionId")]
        public string PositionId { get; set; }

        [JsonProperty("marginMode")]
        public string MarginMode { get; set; }

        [JsonProperty("positionMode")]
        public string PositionMode { get; set; }

        [JsonProperty("side")]
        public string Side { get; set; }

        [JsonProperty("leverage")]
        public string Leverage { get; set; }

        [JsonProperty("margin")]
        public string Margin { get; set; }

        [JsonProperty("ctime")]
        public string Ctime { get; set; }

        [JsonProperty("qty")]
        public string Qty { get; set; }

        [JsonProperty("entryValue")]
        public string EntryValue { get; set; }

        [JsonProperty("symbol")]
        public string Symbol { get; set; }

        [JsonProperty("realizedPNL")]
        public string RealizedPnl { get; set; }

        [JsonProperty("unrealizedPNL")]
        public string UnrealizedPnl { get; set; }

        [JsonProperty("funding")]
        public string Funding { get; set; }

        [JsonProperty("fee")]
        public string Fee { get; set; }
    }

    public class DepthBook
    {
        [JsonProperty("ch")] public string Channel { get; set; }
        [JsonProperty("symbol")] public string Symbol { get; set; }
        [JsonProperty("ts")] public long Timestamp { get; set; }
        [JsonProperty("data")] public DepthData Data { get; set; }
    }

    public class DepthData
    {
        // each entry is [ price, size ]
        [JsonProperty("a")] public List<List<string>> Asks { get; set; }
        [JsonProperty("b")] public List<List<string>> Bids { get; set; }
    }

    public class PriceUpdate
    {
        [JsonProperty("ch")]
        public string Channel { get; set; }

        [JsonProperty("symbol")]
        public string Symbol { get; set; }

        [JsonProperty("ts")]
        public long Timestamp { get; set; }

        [JsonProperty("data")]
        public PriceData Data { get; set; }
    }

    public class FundingUpdate
    {
        [JsonProperty("ch")]
        public string Channel { get; set; }

        [JsonProperty("symbol")]
        public string Symbol { get; set; }

        [JsonProperty("ts")]
        public long Timestamp { get; set; }

        [JsonProperty("data")]
        public FundingRate Data { get; set; }
    }

    public class FundingRate
    {
        public string symbol { get; set; }
        public string utime { get; set; }
        public DateTime fundingTime { get; set; }
        public int fundingInterval { get; set; }
        public string indexPrice { get; set; }
        public string markPrice { get; set; }
        public string fundingRateLast { get; set; }
        public string fundingRatePredict { get; set; }
        public DateTime fundingAt { get; set; }
        public int markPriceTime { get; set; }
    }


    public class PriceData
    {
        [JsonProperty("mp")] public string MarketPrice { get; set; }
        [JsonProperty("ip")] public string IndexPrice { get; set; }
        [JsonProperty("fr")] public string FundingRate { get; set; }
        [JsonProperty("ft")] public string FundingRateSettleTime { get; set; }
        [JsonProperty("nft")] public string NextFundingSettleTime { get; set; }
    }

    public class BitunixTickerData
    {
        [JsonProperty("o")]
        public string Open { get; set; }

        [JsonProperty("h")]
        public string High { get; set; }

        [JsonProperty("l")]
        public string Low { get; set; }

        [JsonProperty("la")]
        public string Last { get; set; }

        [JsonProperty("b")]
        public string BaseVolume { get; set; }

        [JsonProperty("q")]
        public string QuoteVolume { get; set; }

        [JsonProperty("r")]
        public string Change { get; set; }
    }

    public class BitunixTickerItem
    {
        [JsonProperty("s")] public string Symbol { get; set; }
        [JsonProperty("o")] public string Open { get; set; }
        [JsonProperty("h")] public string High { get; set; }
        [JsonProperty("l")] public string Low { get; set; }
        [JsonProperty("la")] public string Last { get; set; }
        [JsonProperty("b")] public string BaseVolume { get; set; }
        [JsonProperty("q")] public string QuoteVolume { get; set; }
        [JsonProperty("r")] public string Change24h { get; set; }
        [JsonProperty("bd")] public string BestBidPrice { get; set; }
        [JsonProperty("ak")] public string BestAskPrice { get; set; }
        [JsonProperty("bv")] public string BestBidVolume { get; set; }
        [JsonProperty("av")] public string BestAskVolume { get; set; }
    }

    public class BitunixTradeData
    {
        [JsonProperty("t")]
        public string Timestamp { get; set; }

        [JsonProperty("p")]
        public string Price { get; set; }

        [JsonProperty("v")]
        public string Volume { get; set; }

        [JsonProperty("s")]
        public string Side { get; set; }
    }

}
