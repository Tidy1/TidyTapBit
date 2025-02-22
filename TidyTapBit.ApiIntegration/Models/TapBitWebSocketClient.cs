using TidyTapBit.Core;
using RestSharp;

namespace TidyTapBit.ApiIntegration.Models
{
    public class TapbitWebSocketClient
    {
        private readonly RestClient _client;

        public TapbitWebSocketClient(string baseUrl)
        {
            _client = new RestClient(baseUrl);
        }

        public async Task ConnectAndSubscribeAsync(string topic)
        {
            var request = new RestRequest("/v1/ws", Method.Get);
            var response = await _client.ExecuteAsync(request);

            if (response.IsSuccessful)
            {
                Console.WriteLine("WebSocket connection established.");

                await SubscribeAsync(topic);

                // Simulate ping-pong mechanism
                _ = Task.Run(() => KeepAliveAsync());
            }
            else
            {
                Console.WriteLine("Failed to establish WebSocket connection.");
            }
        }

        private async Task SubscribeAsync(string topic)
        {
            var request = new RestRequest("/v1/ws/subscribe", Method.Post);
            request.AddJsonBody(new
            {
                channel = topic
            });

            var response = await _client.ExecuteAsync(request);
            if (response.IsSuccessful)
            {
                Console.WriteLine($"Subscribed to topic: {topic}");
            }
            else
            {
                Console.WriteLine($"Failed to subscribe to topic: {response.Content}");
            }
        }

        private async Task KeepAliveAsync()
        {
            while (true)
            {
                var request = new RestRequest("/v1/ws/ping", Method.Post);
                var response = await _client.ExecuteAsync(request);

                if (response.IsSuccessful)
                {
                    Console.WriteLine("Pong sent to server.");
                }
                else
                {
                    Console.WriteLine("Failed to send Pong to server.");
                }

                await Task.Delay(5000); // Send a ping every 5 seconds
            }
        }
    }
}
