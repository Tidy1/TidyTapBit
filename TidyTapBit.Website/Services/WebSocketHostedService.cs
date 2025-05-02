using TidyTapBit.ApiIntegration.Models;

namespace TidyTapBit.Website.Services
{
    public class WebSocketHostedService : IHostedService
    {
        private readonly BitunixWebSocketClient _publicWsClient;
        private readonly BitunixWebSocketClient _privateWsClient;

        public WebSocketHostedService(IEnumerable<BitunixWebSocketClient> wsClients)
        {
            _publicWsClient = wsClients.First(client => client._baseUrl.Contains("public"));
            _privateWsClient = wsClients.First(client => client._baseUrl.Contains("private"));
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await _publicWsClient.ConnectAsync();
            await _publicWsClient.SubscribeAsync(
                ("HBARUSDT", "depth_books")
            );

            await _privateWsClient.ConnectAsync();
            await _privateWsClient.SubscribeAsync(
                ("BTCUSDT", "user_orders"),
                ("BTCUSDT", "user_trades")
            );
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.WhenAll(
                _publicWsClient.CloseAsync(),
                _privateWsClient.CloseAsync()
            );
        }
    }
}
