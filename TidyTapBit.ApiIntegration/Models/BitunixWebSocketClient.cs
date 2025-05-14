using System;
using System.Collections.Generic;
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

        private CancellationTokenSource _pingCts;

        public BitunixWebSocketClient(string apiKey, string apiSecret, string baseUrl)
        {
            _apiKey = apiKey;
            _apiSecret = apiSecret;
            _baseUrl = baseUrl; // e.g. "wss://fapi.bitunix.com/private/"
        }

        private string GenerateSignature(string timestamp, string nonce)
        {
            using var sha256 = SHA256.Create();
            // first: SHA256(nonce + timestamp + apiKey)
            var hash1 = sha256.ComputeHash(Encoding.UTF8.GetBytes(nonce + timestamp + _apiKey));
            var hex1 = BitConverter.ToString(hash1).Replace("-", "").ToLower();
            // then: SHA256(firstHex + secretKey)
            var hash2 = sha256.ComputeHash(Encoding.UTF8.GetBytes(hex1 + _apiSecret));
            return BitConverter.ToString(hash2).Replace("-", "").ToLower();
        }

        public async Task ConnectAsync()
        {
            _webSocket = new ClientWebSocket();
            _webSocket.Options.KeepAliveInterval = TimeSpan.FromSeconds(20);
            // optional: this sends WebSocket‐level pings too
            _cts = new CancellationTokenSource();

            Console.WriteLine($"[WS] Connecting to {_baseUrl}");
            await _webSocket.ConnectAsync(new Uri(_baseUrl), CancellationToken.None);
            Console.WriteLine("[WS] Connected");

            await AuthenticateAsync();

            // start the receive loop
            _ = ReceiveLoop(_cts.Token);

            // start the JSON‐ping loop
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
                    catch
                    {
                        // swallow; receive loop will exit if broken
                    }
                    await Task.Delay(TimeSpan.FromSeconds(30), token);
                }
            }, _pingCts.Token);
        }

        private async Task AuthenticateAsync()
        {
            var ts = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
            var nonce = Guid.NewGuid().ToString("N").Substring(0, 32);
            var sign = GenerateSignature(ts, nonce);

            var login = new
            {
                op = "login",
                args = new[] { new { apiKey = _apiKey, timestamp = ts, nonce, sign } }
            };
            var msg = JsonConvert.SerializeObject(login);
            Console.WriteLine($"[WS TX] {msg}");
            var buf = Encoding.UTF8.GetBytes(msg);
            await _webSocket.SendAsync(buf, WebSocketMessageType.Text, true, CancellationToken.None);
        }



        public async Task SubscribeToBalanceAsync()
        {
            var sub = new
            {
                op = "subscribe",
                args = new[] { new { ch = "position" } }
            };
            var msg = JsonConvert.SerializeObject(sub);
            Console.WriteLine($"[WS TX] {msg}");
            var buf = Encoding.UTF8.GetBytes(msg);
            await _webSocket.SendAsync(buf, WebSocketMessageType.Text, true, CancellationToken.None);
        }

        public async Task SubscribeToOrderAsync(string symbol = null)
        {
            var args = new List<object> { new { ch = "order", symbol } };
            var msgObj = new { op = "subscribe", args = args.ToArray() };
            var msg = JsonConvert.SerializeObject(msgObj);
            Console.WriteLine($"[WS TX] {msg}");
            var buf = Encoding.UTF8.GetBytes(msg);
            await _webSocket.SendAsync(buf, WebSocketMessageType.Text, true, CancellationToken.None);
        }

        public async Task SubscribeToPositionAsync()
        {
            // exactly like SubscribeToBalance but raises a different event
            var sub = new
            {
                op = "subscribe",
                args = new[] { new { ch = "position" } }
            };
            var msg = JsonConvert.SerializeObject(sub);
            Console.WriteLine($"[WS TX] {msg}");
            var buf = Encoding.UTF8.GetBytes(msg);
            await _webSocket.SendAsync(buf, WebSocketMessageType.Text, true, CancellationToken.None);
        }

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
            var sub = new
            {
                op = "subscribe",
                args = new[]
                {
                    new { symbol = symbol, ch = "price" }
                }
            };
            var msg = JsonConvert.SerializeObject(sub);
            Console.WriteLine($"[WS TX] {msg}");
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
                args = new[]
                {
                    new { symbol = symbol, ch = "trade" }
                }
            };
            var json = JsonConvert.SerializeObject(msg);
            Console.WriteLine($"[WS TX] {json}");
            await _webSocket.SendAsync(
                Encoding.UTF8.GetBytes(json),
                WebSocketMessageType.Text,
                true,
                CancellationToken.None
            );
        }


        public async Task SendPingAsync()
        {
            var ping = new { op = "ping", ping = DateTimeOffset.UtcNow.ToUnixTimeSeconds() };
            var msg = JsonConvert.SerializeObject(ping);
            Console.WriteLine($"[WS TX] {msg}");
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
            Console.WriteLine("[WS] Closed");
        }

        private async Task ReceiveLoop(CancellationToken ct)
        {
            var buf = new byte[8 * 1024];
            try
            {
                while (!ct.IsCancellationRequested && _webSocket.State == WebSocketState.Open)
                {
                    var res = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buf), ct);
                    if (res.MessageType == WebSocketMessageType.Close)
                    {
                        Console.WriteLine("[WS] Closed by server");
                        await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Acknowledge close", CancellationToken.None);
                        break;
                    }
                    var msg = Encoding.UTF8.GetString(buf, 0, res.Count);
                    Console.WriteLine($"[WS RX] {msg}");
                    HandleMessage(msg);
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                Console.WriteLine($"[WS ERR] {ex}");
            }
        }

        private void HandleMessage(string message)
        {
            try
            {
                var j = JObject.Parse(message);

                // pong
                if (j["op"]?.ToString() == "pong")
                {
                    var ts = j["pong"]?.ToObject<long>() ?? 0;
                    OnPong?.Invoke(ts);
                    return;
                }

                // connect ack
                if (j["op"]?.ToString() == "connect")
                {
                    return;
                }

                var ch = j["ch"]?.ToString();
                switch (ch)
                {
                    // both balance‐push and position‐push share ch="position",
                    // so we distinguish by object shape:
                    case "position":
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
                        var orders = j["data"]?.ToObject<List<BitunixOrderData>>();
                        if (orders != null) OnOrderUpdate?.Invoke(orders);
                        break;

                    case "depth_books":
                    case "depth_book1":
                    case "depth_book5":
                    case "depth_book15":
                        var book = j.ToObject<DepthBook>();
                        OnDepthUpdate?.Invoke(book);
                        break;

                    case "price":
                        var pu = j.ToObject<PriceUpdate>();
                        OnPriceUpdate?.Invoke(pu);
                        break;

                    case "ticker":
                        // single‐symbol rolling‐24h stats
                        var ticker = j["data"]?.ToObject<BitunixTickerData>();
                        if (ticker != null)
                            OnTickerUpdate?.Invoke(ticker);
                        break;

                    case "tickers":
                        var list = j["data"]?.ToObject<List<BitunixTickerItem>>();
                        if (list != null)
                        {
                            Console.WriteLine("=== Aggregated Tickers Update ===");
                            foreach (var t in list)
                                Console.WriteLine($"{t.Symbol} → Last:{t.Last} 24h∆:{t.Change24h}");
                            OnTickersUpdate?.Invoke(list);
                        }
                        break;

                    case "trade":
                        var trades = j["data"]?.ToObject<List<BitunixTradeData>>();
                        if (trades != null)
                        {
                            Console.WriteLine("=== Trade Update ===");
                            foreach (var t in trades)
                                Console.WriteLine($"{t.Timestamp} {t.Side.ToUpper(),4} {t.Volume} @ {t.Price}");
                            OnTradeUpdate?.Invoke(trades);
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[WS ERR] {ex.Message}\n{message}");
            }
        }

        public void Dispose()
        {
            _cts?.Cancel();
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
