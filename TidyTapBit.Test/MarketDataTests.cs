using System;
using System.Threading.Tasks;
using Xunit;
using TidyTrader.ApiIntegration.Models;
using TidyTrader.ApiIntegration.Interfaces;
using Skender.Stock.Indicators;
using TidyTapBit.Core.Indicators;

namespace TidyTrader.Tests.ApiIntegration
{
    public class MarketDataTests
    {
        private readonly MarketData _marketData;

        public MarketDataTests()
        {
            var apiKey = "dd9ac82aaedec750922f3e6fc5438816";
            var apiSecret = "4ac673c254b5affa65549a2ed5f25c76";
            var client = new BitunixApiClient(apiKey, apiSecret);
            var leverageConfig = new LeverageConfig(1,20);
            _marketData = new MarketData(client, leverageConfig);
        }

        [Fact]
        public async Task GetOrderBookAsync_Works()
        {
            var result = await _marketData.GetOrderBookAsync("BTCUSDT", "max");
            Assert.NotNull(result);
        }

        [Fact]
        public async Task GetKlineDataAsync_Works()
        {
            var result = await _marketData.GetKlineDataAsync("BTCUSDT", "1d", "1000");

            var quotes = result.Data
                .OrderBy(x => x.Time)
                .Select(x => new Quote
                {
                    Close = x.Close,
                    High = x.High,
                    Low = x.Low,
                    Open = x.Open,
                    Volume = decimal.Parse(x.BaseVolume),
                    Date = DateTimeOffset.FromUnixTimeMilliseconds(x.Time).UtcDateTime,
                });

            var indicatorService = new TechnicalIndicatorService();
            var results = indicatorService.GetIndicators(quotes);

            Assert.NotNull(result);
        }

        [Fact]
        public async Task GetTickerAsync_Works()
        {
            var result = await _marketData.GetTickerAsync("BTCUSDT");
            Assert.NotNull(result);
        }

        [Fact]
        public async Task GetFundingRateAsync_Works()
        {
            var result = await _marketData.GetFundingRateAsync("BTCUSDT");
            Assert.NotNull(result);
        }

        [Fact]
        public async Task GetPendingTradesAsync_Works()
        {
            var result = await _marketData.GetPendingTradesAsync("BTCUSDT");
            Assert.NotNull(result);
        }


        [Fact]
        public async Task GetRecentTradesAsync_Works()
        {
            var start = DateTimeOffset.UtcNow.AddDays(-1).ToUnixTimeMilliseconds();
            var result = await _marketData.GetRecentTradesAsync("BTCUSDT", start, null, 100);
            Assert.NotNull(result);
        }

        [Fact]
        public async Task GetLivePriceAsync_Works()
        {
            var result = await _marketData.GetLivePriceAsync("BTCUSDT");
            Assert.True(result > 0);
        }

        [Fact]
        public async Task GetVwapAsync_ReturnsValue()
        {
            var result = await _marketData.GetVwapAsync("BTCUSDT");
            Assert.True(result > 0);
        }

        [Fact]
        public async Task GetPriceMomentumAsync_ReturnsValue()
        {
            var result = await _marketData.GetPriceMomentumAsync("BTCUSDT");
            Assert.True(result != 0);
        }

        [Fact]
        public async Task GetBollingerBandsAsync_ReturnsValidBands()
        {
            var (upper, lower) = await _marketData.GetBollingerBandsAsync("BTCUSDT", "1h",20);
            Assert.True(upper > lower);
        }

        [Fact]
        public async Task GetSmaAsync_ReturnsValue()
        {
            var result = await _marketData.GetSmaAsync("BTCUSDT");
            Assert.True(result > 0);
        }

        [Fact]
        public async Task GetRsiAsync_ReturnsValue()
        {
            var result = await _marketData.GetRsiAsync("BTCUSDT");
            Assert.InRange(result, 0, 100);
        }

        [Fact]
        public async Task DetectCandlePatternAsync_ReturnsPattern()
        {
            var result = await _marketData.DetectCandlePatternAsync("BTCUSDT");
            Assert.False(string.IsNullOrWhiteSpace(result));
        }
    }

}

