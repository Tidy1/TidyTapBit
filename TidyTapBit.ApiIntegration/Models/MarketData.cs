using RestSharp;
using Newtonsoft.Json.Linq;
using TidyTrader.ApiIntegration.Interfaces;

namespace TidyTrader.ApiIntegration.Models
{
    public class MarketData : IMarketData
    {
        private readonly IBitunixApiClient _bitunixClient;
        private LeverageConfig _leverageConfig;

        public MarketData(IBitunixApiClient bitunixClient, LeverageConfig leverageConfig)
        {
            _bitunixClient = bitunixClient;
            _leverageConfig = leverageConfig;
        }

        public void SetLeverageConfig(int minLeverage, int maxLeverage)
        {
            _leverageConfig.MinLeverage = minLeverage;
            _leverageConfig.MaxLeverage = maxLeverage;
        }

        public LeverageConfig GetLeverageConfig() => _leverageConfig;

        private int DetermineLeverage()
        {
            var random = new Random();
            return random.Next(_leverageConfig.MinLeverage, _leverageConfig.MaxLeverage + 1);
        }

        public async Task<string> GetExchangeInfoAsync() => "none";

        public async Task<string> GetOrderBookAsync(string symbol, string limit)
        {
            var response = await _bitunixClient.GetMarketDepthAsync(symbol, limit);
            return response.Content;
        }

        public async Task<string> GetKlineDataAsync(string symbol, string interval, string limit)
        {
            var response = await _bitunixClient.GetKlineAsync(symbol, interval, null, null, int.Parse(limit), null);
            return response.Content;
        }

        public async Task<string> GetTickerAsync(string symbol)
        {
            var response = await _bitunixClient.GetTickersAsync(symbol);
            return response.Content;
        }

        public async Task<string> GetFundingRateAsync(string symbol)
        {
            var response = await _bitunixClient.GetFundingRateAsync(symbol);
            return response.Content;
        }

        public async Task<string> GetTickerListAsync()
        {
            var response = await _bitunixClient.GetTickersAsync();
            return response.Content;
        }

        public async Task<string> GetRecentTradesAsync(string? symbol = null, long? startTime = null, long? endTime = null, int? limit = null)
        {
            var response = await _bitunixClient.GetHistoryTradesAsync(symbol, null, null, startTime, endTime, null, limit);
            return response.Content;
        }

        public async Task<decimal> GetLivePriceAsync(string symbol)
        {
            var content = await GetTickerAsync(symbol);
            var json = JToken.Parse(content);

            if (json is JArray arr && arr.Count > 0)
                return decimal.Parse(arr[0]["last"].ToString());

            return 0m;
        }

        public async Task<decimal> GetMovingAverageAsync(string symbol, string interval, int period)
        {
            var content = await GetKlineDataAsync(symbol, interval, period.ToString());
            var data = JArray.Parse(content);

            if (data.Count < period) return 0m;

            var sum = data.Take(period)
                          .Select(kline => decimal.Parse(kline[4].ToString())) // close price
                          .Sum();

            return sum / period;
        }

        public async Task<decimal> GetOrderBookSpreadAsync(string symbol)
        {
            var content = await GetOrderBookAsync(symbol, "5");
            var book = JObject.Parse(content);

            var bestBid = decimal.Parse(book["bids"]?[0]?[0]?.ToString() ?? "0");
            var bestAsk = decimal.Parse(book["asks"]?[0]?[0]?.ToString() ?? "0");

            return bestAsk - bestBid;
        }

        public async Task<(decimal bidVolume, decimal askVolume)> GetOrderBookVolumesAsync(string symbol)
        {
            var content = await GetOrderBookAsync(symbol, "5");
            var book = JObject.Parse(content);

            decimal bidVolume = book["bids"]?
                .Take(5)
                .Sum(bid => decimal.Parse(bid[1]?.ToString() ?? "0")) ?? 0;

            decimal askVolume = book["asks"]?
                .Take(5)
                .Sum(ask => decimal.Parse(ask[1]?.ToString() ?? "0")) ?? 0;

            return (bidVolume, askVolume);
        }

        public async Task<RestResponse> PlaceIsolatedTradeAsync(string symbol, string qty, string side, string orderType)
        {
            var leverage = DetermineLeverage().ToString();
            return await _bitunixClient.PlaceOrderAsync(symbol, qty, side, "OPEN", orderType);
        }

        public Task<DateTime> GetServerTimeAsync() => throw new NotImplementedException();
    }
}
