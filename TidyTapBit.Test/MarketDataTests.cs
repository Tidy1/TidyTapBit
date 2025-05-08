using System;
using System.Threading.Tasks;
using Xunit;
using TidyTrader.ApiIntegration.Models;
using TidyTrader.ApiIntegration.Interfaces;

namespace TidyTrader.Tests.ApiIntegration
{
    public class MarketDataTests
    {
        private const string apiKey = "dd9ac82aaedec750922f3e6fc5438816";
        private const string apiSecret = "4ac673c254b5affa65549a2ed5f25c76";
        private readonly IMarketData _marketData;

        public MarketDataTests()
        {
            var leverageConfig = new LeverageConfig(1,20);

            var bitunixClient = new BitunixApiClient(apiKey, apiSecret);
            _marketData = new MarketData(bitunixClient, leverageConfig);
        }

        [Fact]
        public async Task GetLivePrice_ShouldReturn_PositiveValue()
        {
            var price = await _marketData.GetLivePriceAsync("BTCUSDT");
            Assert.True(price > 0, "Expected price to be greater than 0");
        }

        [Fact]
        public async Task GetMovingAverage_ShouldReturn_Value()
        {
            var average = await _marketData.GetMovingAverageAsync("BTCUSDT", "1m", 5);
            Assert.True(average > 0, "Expected moving average to be greater than 0");
        }

        [Fact]
        public async Task GetOrderBookSpread_ShouldReturn_PositiveSpread()
        {
            var spread = await _marketData.GetOrderBookSpreadAsync("BTCUSDT");
            Assert.True(spread >= 0, "Expected spread to be 0 or positive");
        }

        [Fact]
        public async Task GetOrderBookVolumes_ShouldReturn_BidAndAskVolumes()
        {
            var (bidVolume, askVolume) = await _marketData.GetOrderBookVolumesAsync("BTCUSDT");
            Assert.True(bidVolume >= 0 && askVolume >= 0);
        }

        [Fact]
        public async Task GetTickerList_ShouldReturn_NonEmptyString()
        {
            var result = await _marketData.GetTickerListAsync();
            Assert.False(string.IsNullOrWhiteSpace(result));
        }

        //[Fact]
        public async Task PlaceIsolatedTradeAsync_ShouldReturn_SuccessfulResponse()
        {
            var result = await _marketData.PlaceIsolatedTradeAsync("BTCUSDT", "0.001", "BUY", "MARKET");
            Assert.True(result.IsSuccessful, $"Error: {result.Content}");
        }
    }

}

