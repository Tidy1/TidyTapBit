using System.Security.Cryptography.X509Certificates;

using TidyTapBit.ApiIntegration.Models;

namespace TidyTapBit.Test
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
            string response = await _apiClient.GetMarketTickersAsync();
            Assert.False(string.IsNullOrEmpty(response), "Market tickers API response should not be empty");
            Console.WriteLine(response);
        }

        [Fact]
        public async Task Test_GetTradingPairsAsync()
        {
            string response = await _apiClient.GetTradingPairsAsync();
            Assert.False(string.IsNullOrEmpty(response), "Trading pairs API response should not be empty");
            Console.WriteLine(response);
        }

        [Fact]
        public async Task Test_GetAccountInfoAsync()
        {
            string marginCoin = "BTC"; // Update with the correct margin coin
            string response = await _apiClient.GetAccountInfoAsync(marginCoin);
            Assert.False(string.IsNullOrEmpty(response), "Account info API response should not be empty");
            Console.WriteLine(response);
        }


        [Fact]
        public async Task Test_GetMarketDepthAsync()
        {
            string symbol = "BTCUSDT"; // Update with the correct margin coin
            var limit = "max";
            string response = await _apiClient.GetMarketDepthAsync(symbol,limit);
            Assert.False(string.IsNullOrEmpty(response), "Account info API response should not be empty");
            Console.WriteLine(response);
        }

                
        [Fact]
        public async Task Test_PlaceFuturesOrderAsync()
        {
            string symbol = "BTCUSDT";
            decimal qty = 0.01m;
            string side = "buy";
            string orderType = "limit";
            int leverage = 20;
            decimal? price = 30000m;
            string clientOrderId = "testOrder123";
            string timeInForce = "GTC";
            bool? reduceOnly = false;
            bool? postOnly = false;
            string positionSide = "LONG";

            string response = await _apiClient.PlaceFuturesOrderAsync(symbol, qty, side, orderType, leverage, price, clientOrderId, timeInForce, reduceOnly, postOnly, positionSide);
            Assert.False(string.IsNullOrEmpty(response), "Futures order API response should not be empty");
            Console.WriteLine(response);
        }

        [Fact]
        public async Task Test_GetFundingRateAsync() // New test
        {
            string symbol = "BTCUSDT";
            string response = await _apiClient.GetFundingRateAsync(symbol);
            Assert.False(string.IsNullOrEmpty(response), "Funding rate API response should not be empty");
            Console.WriteLine(response);
        }

        [Fact]
        public async Task Test_GetKlineAsync() // New test
        {
            string symbol = "BTCUSDT";
            string interval = "1m";
            int limit = 100;
            string response = await _apiClient.GetKlineAsync(symbol, interval, limit);
            Assert.False(string.IsNullOrEmpty(response), "Kline API response should not be empty");
            Console.WriteLine(response);
        }

        [Fact]
        public async Task Test_GetHistoryPositionsAsync() // New test
        {
            string symbol = "BTCUSDT";
            int limit = 100;
            string response = await _apiClient.GetHistoryPositionsAsync(symbol, limit);
            Assert.False(string.IsNullOrEmpty(response), "History positions API response should not be empty");
            Console.WriteLine(response);
        }

        [Fact]
        public async Task Test_GetPendingPositionsAsync() // New test
        {
            string symbol = "BTCUSDT";
            string response = await _apiClient.GetPendingPositionsAsync(symbol);
            Assert.False(string.IsNullOrEmpty(response), "Pending positions API response should not be empty");
            Console.WriteLine(response);
        }

        [Fact]
        public async Task Test_GetPositionTiersAsync() // New test
        {
            string symbol = "BTCUSDT";
            string response = await _apiClient.GetPositionTiersAsync(symbol);
            Assert.False(string.IsNullOrEmpty(response), "Position tiers API response should not be empty");
            Console.WriteLine(response);
        }

        [Fact]
        public async Task Test_CancelTpSlOrderAsync() // New test
        {
            string symbol = "BTCUSDT";
            string orderId = "12345";
            string response = await _apiClient.CancelTpSlOrderAsync(symbol, orderId);
            Assert.False(string.IsNullOrEmpty(response), "Cancel TP/SL order API response should not be empty");
            Console.WriteLine(response);
        }

        [Fact]
        public async Task Test_GetHistoryTpSlOrderAsync() // New test
        {
            string symbol = "BTCUSDT";
            string response = await _apiClient.GetHistoryTpSlOrderAsync(symbol);
            Assert.False(string.IsNullOrEmpty(response), "History TP/SL order API response should not be empty");
            Console.WriteLine(response);
        }

        [Fact]
        public async Task Test_GetPendingTpSlOrderAsync() // New test
        {
            string symbol = "BTCUSDT";
            string response = await _apiClient.GetPendingTpSlOrderAsync(symbol);
            Assert.False(string.IsNullOrEmpty(response), "Pending TP/SL order API response should not be empty");
            Console.WriteLine(response);
        }

        [Fact]
        public async Task Test_ModifyPositionTpSlOrderAsync() // New test
        {
            string symbol = "BTCUSDT";
            string orderId = "12345";
            decimal stopLoss = 30000m;
            decimal takeProfit = 40000m;
            string response = await _apiClient.ModifyPositionTpSlOrderAsync(symbol, orderId, stopLoss, takeProfit);
            Assert.False(string.IsNullOrEmpty(response), "Modify position TP/SL order API response should not be empty");
            Console.WriteLine(response);
        }

        [Fact]
        public async Task Test_ModifyTpSlOrderAsync() // New test
        {
            string symbol = "BTCUSDT";
            string orderId = "12345";
            decimal stopLoss = 30000m;
            decimal takeProfit = 40000m;
            string response = await _apiClient.ModifyTpSlOrderAsync(symbol, orderId, stopLoss, takeProfit);
            Assert.False(string.IsNullOrEmpty(response), "Modify TP/SL order API response should not be empty");
            Console.WriteLine(response);
        }

        [Fact]
        public async Task Test_PlacePositionTpSlOrderAsync() // New test
        {
            string symbol = "BTCUSDT";
            decimal stopLoss = 30000m;
            decimal takeProfit = 40000m;
            string response = await _apiClient.PlacePositionTpSlOrderAsync(symbol, stopLoss, takeProfit);
            Assert.False(string.IsNullOrEmpty(response), "Place position TP/SL order API response should not be empty");
            Console.WriteLine(response);
        }

        [Fact]
        public async Task Test_PlaceTpSlOrderAsync() // New test
        {
            string symbol = "BTCUSDT";
            decimal stopLoss = 30000m;
            decimal takeProfit = 40000m;
            string response = await _apiClient.PlaceTpSlOrderAsync(symbol, stopLoss, takeProfit);
            Assert.False(string.IsNullOrEmpty(response), "Place TP/SL order API response should not be empty");
            Console.WriteLine(response);
        }

        [Fact]
        public async Task Test_CancelAllOrdersAsync() // New test
        {
            string symbol = "BTCUSDT";
            string response = await _apiClient.CancelAllOrdersAsync(symbol);
            Assert.False(string.IsNullOrEmpty(response), "Cancel all orders API response should not be empty");
            Console.WriteLine(response);
        }

        [Fact]
        public async Task Test_CancelOrdersAsync() // New test
        {
            string symbol = "BTCUSDT";
            string[] orderIds = { "12345", "67890" };
            string response = await _apiClient.CancelOrdersAsync(symbol, orderIds);
            Assert.False(string.IsNullOrEmpty(response), "Cancel orders API response should not be empty");
            Console.WriteLine(response);
        }
    }
}