using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Text;
using System.Net.WebSockets;

namespace TidyTrader.ApiIntegration.Models
{
    public class BitunixWebSocketClient
    {
        private readonly string _apiKey;
        private readonly string _apiSecret;
        public readonly string _baseUrl;
        private ClientWebSocket _webSocket;

        public BitunixWebSocketClient(string apiKey, string apiSecret, string baseUrl)
        {
            _apiKey = apiKey;
            _apiSecret = apiSecret;
            _baseUrl = baseUrl;
        }

        private string GenerateSignature(string timestamp, string nonce)
        {
            string preHash = timestamp + nonce;
            using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_apiSecret)))
            {
                byte[] hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(preHash));
                return BitConverter.ToString(hash).Replace("-", "").ToLower();
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
                args = new
                {
                    apiKey = _apiKey,
                    timestamp,
                    nonce,
                    sign = signature
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
            var buffer = new byte[1024 * 4];
            while (_webSocket.State == WebSocketState.Open)
            {
                var result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
                }
                else
                {
                    var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    //Console.WriteLine("Received: " + message);
                }
            }
        }
    }
}