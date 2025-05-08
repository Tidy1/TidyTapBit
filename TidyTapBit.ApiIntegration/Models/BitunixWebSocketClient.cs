using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Text;
using System.Net.WebSockets;
using Newtonsoft.Json.Linq;

namespace TidyTrader.ApiIntegration.Models
{
    public class BitunixWebSocketClient
    {
        private readonly string _apiKey;
        private readonly string _apiSecret;
        public readonly string _baseUrl;
        private ClientWebSocket _webSocket;
        public WebSocketState WebSocketState => _webSocket?.State ?? WebSocketState.None;

        public event Action<List<BitunixBalanceData>> OnBalanceUpdate;

        public BitunixWebSocketClient(string apiKey, string apiSecret, string baseUrl)
        {
            _apiKey = apiKey;
            _apiSecret = apiSecret;
            _baseUrl = baseUrl;
        }

        private string GenerateSignature(string timestamp, string nonce)
        {
            string preHash = nonce + timestamp + _apiKey;
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] digest = sha256.ComputeHash(Encoding.UTF8.GetBytes(preHash));
                string firstHash = BitConverter.ToString(digest).Replace("-", "").ToLower();

                byte[] final = sha256.ComputeHash(Encoding.UTF8.GetBytes(firstHash + _apiSecret));
                return BitConverter.ToString(final).Replace("-", "").ToLower();
            }
        }


        public async Task ConnectAsync()
        {
            _webSocket = new ClientWebSocket();
            await _webSocket.ConnectAsync(new Uri(_baseUrl), CancellationToken.None);
            Console.WriteLine("WebSocket connection opened.");
            await AuthenticateAsync();
            _ = ReceiveMessagesAsync(); // Start receiving messages without awaiting
        }

        private async Task AuthenticateAsync()
        {
            string timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
            string nonce = Guid.NewGuid().ToString("N").Substring(0, 32);
            string signature = GenerateSignature(timestamp, nonce);

            var authMessage = new
            {
                op = "login",
                args = new[]
                {
                    new {
                        apiKey = _apiKey,
                        timestamp,
                        nonce,
                        sign = signature
                    }
                }
            };

            await SendMessageAsync(authMessage);
        }

        public async Task SubscribeAsync(params (string symbol, string channel)[] subscriptions)
        {
            var subscribeMessage = new
            {
                op = "subscribe",
                args = subscriptions.Select(sub => new { sub.symbol, ch = sub.channel }).ToArray()
            };

            await SendMessageAsync(subscribeMessage);
        }

        public async Task UnsubscribeAsync(params (string symbol, string channel)[] subscriptions)
        {
            var unsubscribeMessage = new
            {
                op = "unsubscribe",
                args = subscriptions.Select(sub => new { sub.symbol, ch = sub.channel }).ToArray()
            };

            await SendMessageAsync(unsubscribeMessage);
        }

        public async Task CloseAsync()
        {
            await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
            Console.WriteLine("WebSocket connection closed.");
        }

        private async Task SendMessageAsync(object message)
        {
            string messageJson = Newtonsoft.Json.JsonConvert.SerializeObject(message);
            byte[] messageBytes = Encoding.UTF8.GetBytes(messageJson);
            await _webSocket.SendAsync(new ArraySegment<byte>(messageBytes), WebSocketMessageType.Text, true, CancellationToken.None);
        }
        private async Task ReceiveMessagesAsync()
        {
            var buffer = new byte[8192];

            while (_webSocket.State == WebSocketState.Open)
            {
                var result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
                    break;
                }

                var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                HandleMessage(message);
            }
        }

        private void HandleMessage(string message)
        {
            try
            {
                var jObject = JObject.Parse(message);
                var channel = jObject["ch"]?.ToString();

                switch (channel)
                {
                    case "balance":
                        var balanceList = jObject["data"]?.ToObject<List<BitunixBalanceData>>();
                        OnBalanceUpdate?.Invoke(balanceList);
                        break;
                        // Add cases for other channels like position, order, etc.
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error handling message: {ex.Message}\n{message}");
            }
        }


        public async Task SendPingAsync()
        {
            var pingMessage = new
            {
                op = "ping",
                ping = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            };

            await SendMessageAsync(pingMessage);
        }


        public async Task SubscribeToBalanceAsync()
        {
            var subscribeMessage = new
            {
                op = "subscribe",
                args = new[]
                {
            new { ch = "balance" }
        }
            };

            await SendMessageAsync(subscribeMessage);
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
}