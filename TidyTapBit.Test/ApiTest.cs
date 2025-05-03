using System;
using System.Threading.Tasks;
using Xunit;
using TidyTrader.ApiIntegration.Models;

namespace TidyTrader.Tests.ApiIntegration
{
    public class BitunixApiClientTests
    {
        private const string ApiKey = "dd9ac82aaedec750922f3e6fc5438816";
        private const string ApiSecret = "4ac673c254b5affa65549a2ed5f25c76";
        private readonly BitunixApiClient _client;

        public BitunixApiClientTests()
        {
            // Initialize the BitunixApiClient with test API key and secret
            _client = new BitunixApiClient(ApiKey, ApiSecret);
        }

        [Fact]
        public async Task CancelAllOrdersAsync_ShouldReturnSuccessResponse()
        {
            // Arrange
            string testSymbol = "BTCUSDT";

            // Act
            string response = await _client.CancelAllOrdersAsync(testSymbol);

            // Assert
            Assert.NotNull(response);
            Assert.Contains("success", response, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task CancelOrdersAsync_ShouldReturnSuccessResponse()
        {
            // Arrange
            string testSymbol = "BTCUSDT";
            string[] orderIds = { "order1", "order2" };

            // Act
            string response = await _client.CancelOrdersAsync(testSymbol, orderIds);

            // Assert
            Assert.NotNull(response);
            Assert.Contains("success", response, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task GetAccountInfoAsync_ShouldReturnAccountInfo()
        {
            // Arrange
            string marginCoin = "USDT";

            // Act
            string response = await _client.GetAccountInfoAsync(marginCoin);

            // Assert
            Assert.NotNull(response);
            Assert.Contains("account", response, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task GetFundingRateAsync_ShouldReturnFundingRate()
        {
            // Arrange
            string symbol = "BTCUSDT";

            // Act
            string response = await _client.GetFundingRateAsync(symbol);

            // Assert
            Assert.NotNull(response);
            Assert.Contains("fundingRate", response, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task PlaceFuturesOrderAsync_ShouldReturnOrderResponse()
        {
            // Arrange
            string symbol = "BTCUSDT";
            decimal qty = 0.01m;
            string side = "buy";
            string orderType = "market";
            int leverage = 10;

            // Act
            string response = await _client.PlaceFuturesOrderAsync(symbol, qty, side, orderType, leverage);

            // Assert
            Assert.NotNull(response);
            Assert.Contains("orderId", response, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task GetMarketTickersAsync_ShouldReturnTickers()
        {
            // Arrange
            string symbol = "BTCUSDT";

            // Act
            string response = await _client.GetMarketTickersAsync(symbol);

            // Assert
            Assert.NotNull(response);
            Assert.Contains("tickers", response, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task GetServerTimeAsync_ShouldReturnServerTime()
        {
            // Act
            string response = await _client.GetServerTimeAsync();

            // Assert
            Assert.NotNull(response);
            Assert.Contains("serverTime", response, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task GetTradingPairsAsync_ShouldReturnTradingPairs()
        {
            // Act
            string response = await _client.GetTradingPairsAsync();

            // Assert
            Assert.NotNull(response);
            Assert.Contains("tradingPairs", response, StringComparison.OrdinalIgnoreCase);
        }
    }
}
