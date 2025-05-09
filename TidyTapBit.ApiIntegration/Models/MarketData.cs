using RestSharp;
using Newtonsoft.Json.Linq;
using TidyTrader.ApiIntegration.Interfaces;
using TidyTrader.ApiIntegration.Models.Responses.Market;
using TidyTrader.ApiIntegration.Models.Responses.Trade;
using Newtonsoft.Json;

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

        public async Task<MarketDepthResponse> GetOrderBookAsync(string symbol, string limit)
        {
            var response = await _bitunixClient.GetMarketDepthAsync(symbol, limit);
            return response.Data;
        }

        public async Task<KlineResponse> GetKlineDataAsync(string symbol, string interval, string limit)
        {
            var response = await _bitunixClient.GetKlineAsync(symbol, interval, null, null, int.Parse(limit), null);
            return response.Data;
        }

        public async Task<TickerResponse> GetTickerAsync(string symbol)
        {
            var response = await _bitunixClient.GetTickersAsync(symbol);
            return response.Data;
        }

        public async Task<FundingRateResponse> GetFundingRateAsync(string symbol)
        {
            var response = await _bitunixClient.GetFundingRateAsync(symbol);
            return response.Data;
        }


        public async Task<GetTradeHistoryResponse> GetRecentTradesAsync(string? symbol = null, long? startTime = null, long? endTime = null, int? limit = null)
        {
            var response = await _bitunixClient.GetHistoryTradesAsync(symbol, null, null, startTime, endTime, null, limit);
            return response.Data;
        }

        public async Task<decimal> GetLivePriceAsync(string symbol)
        {
            var content = await GetTickerAsync(symbol);
            
            decimal.TryParse(content.Data.First().Last, out decimal result);

            return result;
        }

      

        public Task<DateTime> GetServerTimeAsync() => throw new NotImplementedException();

        Task<string> IMarketData.GetFundingRateAsync(string symbol)
        {
            throw new NotImplementedException();
        }

        public async Task<KlineResponse> GetKlineDataAsync(string symbol, string interval, string limit)
        {
            if (!int.TryParse(limit, out var parsedLimit))
                throw new ArgumentException("Limit must be an integer", nameof(limit));

            var response = await _bitunixClient.GetKlineAsync(symbol, interval, null, null, parsedLimit, null);

            if (response.Data == null)
                throw new Exception($"Failed to get kline data: {response.ErrorMessage ?? "Unknown error"}");

            return response.Data;
        }

        public Task<decimal> GetMovingAverageAsync(string symbol, string interval, int period)
        {
            throw new NotImplementedException();
        }

        Task<string> IMarketData.GetOrderBookAsync(string symbol, string limit)
        {
            throw new NotImplementedException();
        }

        public Task<decimal> GetOrderBookSpreadAsync(string symbol)
        {
            throw new NotImplementedException();
        }

        public Task<(decimal bidVolume, decimal askVolume)> GetOrderBookVolumesAsync(string symbol)
        {
            throw new NotImplementedException();
        }

        Task<string> IMarketData.GetRecentTradesAsync(string? symbol, long? startTime, long? endTime, int? limit)
        {
            throw new NotImplementedException();
        }

        Task<string> IMarketData.GetTickerAsync(string symbol)
        {
            throw new NotImplementedException();
        }

        public Task<string> GetTickerListAsync()
        {
            throw new NotImplementedException();
        }

        public Task<RestResponse> PlaceIsolatedTradeAsync(string symbol, string qty, string side, string orderType)
        {
            throw new NotImplementedException();
        }
    }
}
