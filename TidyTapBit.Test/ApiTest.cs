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
        public async Task GetAccountInfoAsync_ShouldReturnAccountInfo()
        {
            // Arrange
            string marginCoin = "USDT";

            // Act
            var response = await _client.GetAccountInfoAsync(marginCoin);

            // Assert
            Assert.NotNull(response);   
            Assert.True(response.IsSuccessful);
        }
    }
}
