using System.Security.Cryptography.X509Certificates;

using TidyTrader.ApiIntegration.Models;

namespace TidyTrader.Test
{
    public class BitunixApiClientTests
    {
        private readonly BitunixApiClient _apiClient;

        public BitunixApiClientTests()
        {
            string apiKey = "dd9ac82aaedec750922f3e6fc5438816";
            string apiSecret = "4ac673c254b5affa65549a2ed5f25c76";
            _apiClient = new BitunixApiClient(apiKey, apiSecret);
        }

        [Fact]
        public async Task Test_GetMarketTickersAsync()
        {
            var response = await _apiClient.GetTickersAsync();
            Assert.False(string.IsNullOrEmpty(response.Content), "Market tickers API response should not be empty");
            Console.WriteLine(response);
        }

        [Fact]
        public async Task Test_GetTradingPairsAsync()
        {
            var response = await _apiClient.GetTradingPairsAsync();
            Assert.False(string.IsNullOrEmpty(response.Content), "Trading pairs API response should not be empty");
            Console.WriteLine(response);
        }

        [Fact]
        public async Task Test_GetAccountInfoAsync()
        {
            string marginCoin = "BTC"; // Update with the correct margin coin
            var response = await _apiClient.GetAccountInfoAsync(marginCoin);
            Assert.False(string.IsNullOrEmpty(response.Content), "Account info API response should not be empty");
            Console.WriteLine(response);
        }


        [Fact]
        public async Task Test_GetMarketDepthAsync()
        {
            string symbol = "BTCUSDT"; // Update with the correct margin coin
            var limit = "max";
            var response = await _apiClient.GetMarketDepthAsync(symbol, limit);
            Assert.False(string.IsNullOrEmpty(response.Content), "Account info API response should not be empty");
            Console.WriteLine(response);
        }


        [Fact]
        public async Task Test_PlaceFuturesOrderAsync()
        {
            string symbol = "BTCUSDT";
            var qty = "0.01";
            string side = "buy";
            string orderType = "limit";
            var leverage = "0.01";
            var price = "30000";
            string clientOrderId = "testOrder123";
            string timeInForce = "GTC";
            string positionId = "";
            bool? reduceOnly = false;
            bool? postOnly = false;
            string positionSide = "LONG";

            var response = await _apiClient.PlaceOrderAsync(symbol, qty, side, orderType, leverage, price, clientOrderId, timeInForce, positionId, postOnly, positionSide);
            Assert.False(string.IsNullOrEmpty(response.Content), "Futures order API response should not be empty");
            Console.WriteLine(response);
        }

        [Fact]
        public async Task Test_GetFundingRateAsync() // New test
        {
            string symbol = "BTCUSDT";
            var response = await _apiClient.GetFundingRateAsync(symbol);
            Assert.False(string.IsNullOrEmpty(response.Content), "Funding rate API response should not be empty");
            Console.WriteLine(response);
        }

        [Fact]
        public async Task Test_GetKlineAsync() // New test
        {
            string symbol = "BTCUSDT";
            string interval = "1m";
            int limit = 100;
            var response = await _apiClient.GetKlineAsync(symbol, interval, limit);
            Assert.False(string.IsNullOrEmpty(response.Content), "Kline API response should not be empty");
            Console.WriteLine(response);
        }

        [Fact]
        public async Task Test_GetHistoryPositionsAsync() // New test
        {
            string symbol = "BTCUSDT";
            var positionId = "100";
            var response = await _apiClient.GetHistoryPositionsAsync(symbol, positionId);
            Assert.False(string.IsNullOrEmpty(response.Content), "History positions API response should not be empty");
            Console.WriteLine(response);
        }

        [Fact]
        public async Task Test_GetPendingPositionsAsync() // New test
        {
            string symbol = "BTCUSDT";
            var response = await _apiClient.GetPendingPositionsAsync(symbol);
            Assert.False(string.IsNullOrEmpty(response.Content), "Pending positions API response should not be empty");
            Console.WriteLine(response);
        }

        [Fact]
        public async Task Test_GetPositionTiersAsync() // New test
        {
            string symbol = "BTCUSDT";
            var response = await _apiClient.GetPositionTiersAsync(symbol);
            Assert.False(string.IsNullOrEmpty(response.Content), "Position tiers API response should not be empty");
            Console.WriteLine(response);
        }

        [Fact]
        public async Task Test_CancelTpSlOrderAsync() // New test
        {
            string symbol = "BTCUSDT";
            string orderId = "12345";
            var response = await _apiClient.CancelTpSlOrderAsync(symbol, orderId);
            Assert.False(string.IsNullOrEmpty(response.Content), "Cancel TP/SL order API response should not be empty");
            Console.WriteLine(response);
        }

        [Fact]
        public async Task Test_GetHistoryTpSlOrderAsync() // New test
        {
            string symbol = "BTCUSDT";
            var response = await _apiClient.GetTpSlOrderHistoryAsync(symbol);
            Assert.False(string.IsNullOrEmpty(response.Content), "History TP/SL order API response should not be empty");
            Console.WriteLine(response);
        }

        [Fact]
        public async Task Test_GetPendingTpSlOrderAsync() // New test
        {
            string symbol = "BTCUSDT";
            var response = await _apiClient.GetPendingTpSlOrdersAsync(symbol);
            Assert.False(string.IsNullOrEmpty(response.Content), "Pending TP/SL order API response should not be empty");
            Console.WriteLine(response);
        }

        [Fact]
        public async Task Test_ModifyPositionTpSlOrderAsync() // New test
        {
            string symbol = "BTCUSDT";
            string orderId = "12345";

            var tpStopType = "12345";
            var tpPrice = "Sld";

            var response = await _apiClient.ModifyPositionTpSlOrderAsync(symbol, orderId, tpPrice, tpStopType);
            Assert.False(string.IsNullOrEmpty(response.Content), "Modify position TP/SL order API response should not be empty");
            Console.WriteLine(response);
        }

        [Fact]
        public async Task Test_ModifyTpSlOrderAsync() // New test
        {
            string symbol = "BTCUSDT";
            string orderId = "12345";
            var tpStopType = "12345";
            var tpOrderType = "Sld";
            var response = await _apiClient.ModifyTpSlOrderAsync(symbol, orderId, tpStopType, tpOrderType);
            Assert.False(string.IsNullOrEmpty(response.Content), "Modify TP/SL order API response should not be empty");
            Console.WriteLine(response);
        }

        [Fact]
        public async Task Test_PlacePositionTpSlOrderAsync() // New test
        {
            string symbol = "BTCUSDT";
            var positionId = "12345";
            var tpPrice = "";
            var response = await _apiClient.PlacePositionTpSlOrderAsync(symbol, positionId, tpPrice);
            Assert.False(string.IsNullOrEmpty(response.Content), "Place position TP/SL order API response should not be empty");
            Console.WriteLine(response);
        }

        [Fact]
        public async Task Test_PlaceTpSlOrderAsync() // New test
        {
            string symbol = "BTCUSDT";
            var positionId = "12345";
            var tpPrice = "";

            var response = await _apiClient.PlaceTpSlOrderAsync(symbol, positionId, tpPrice);
            Assert.False(string.IsNullOrEmpty(response.Content), "Place TP/SL order API response should not be empty");
            Console.WriteLine(response);
        }

        [Fact]
        public async Task Test_CancelAllOrdersAsync() // New test
        {
            string symbol = "BTCUSDT";
            var response = await _apiClient.CancelAllOrdersAsync(symbol);
            Assert.False(string.IsNullOrEmpty(response.Content), "Cancel all orders API response should not be empty");
            Console.WriteLine(response);
        }

        [Fact]
        public async Task Test_CancelOrdersAsync() // New test
        {
            string symbol = "BTCUSDT";
            string[] orderIds = { "12345", "67890" };
            var response = await _apiClient.CancelOrdersAsync(symbol, orderIds);
            Assert.False(string.IsNullOrEmpty(response.Content), "Cancel orders API response should not be empty");
            Console.WriteLine(response);
        }

        [Fact]
        public async Task WebSocket_ShouldConnect_And_Subscribe()
        {
            string apiKey = "dd9ac82aaedec750922f3e6fc5438816";
            string apiSecret = "4ac673c254b5affa65549a2ed5f25c76";
            string baseUrl = "wss://fapi.bitunix.com/public/";

            var client = new BitunixWebSocketClient(apiKey, apiSecret, baseUrl);

            await client.ConnectAsync();

            Assert.True(client.WebSocketState == System.Net.WebSockets.WebSocketState.Open);

            await client.SubscribeAsync(("BTCUSDT", "depth_books"));

            // Wait a few seconds to receive data
            await Task.Delay(3000);

            await client.CloseAsync();
        }
    }
}