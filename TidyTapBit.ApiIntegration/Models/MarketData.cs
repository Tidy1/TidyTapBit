using RestSharp;

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

        public LeverageConfig GetLeverageConfig()
        {
            return _leverageConfig;
        }

        private int DetermineLeverage()
        {
            // Implement your logic to determine the leverage
            // For example, you can use a random value between min and max leverage
            Random random = new Random();
            return random.Next(_leverageConfig.MinLeverage, _leverageConfig.MaxLeverage + 1);
        }

        public async Task<RestResponse> PlaceIsolatedTradeAsync(string symbol, string qty, string side, string orderType)
        {
            var leverage = DetermineLeverage().ToString();
            return await _bitunixClient.PlaceOrderAsync(symbol, qty, side, orderType, leverage);
        }

        //public async Task<DateTime> GetServerTimeAsync()
        //{
        //    var response = await _bitunixClient.GetServerTimeAsync();
        //    // Parse the response to get the server time
        //    return DateTime.Parse(response);
        //}

        public async Task<string> GetExchangeInfoAsync()
        {
            return "none";
            //return await _bitunixClient.GetExchangeInfoAsync();
        }

        public async Task<RestResponse> GetOrderBookAsync(string symbol, string limit)
        {
            return await _bitunixClient.GetMarketDepthAsync(symbol, limit);
        }

        public async Task<RestResponse> GetKlineDataAsync(string symbol, string interval, string limit)
        {
            return await _bitunixClient.GetKlineAsync(symbol, interval);
        }

        public async Task<RestResponse> GetTickerAsync(string? symbol)
        {
            return await _bitunixClient.GetTickersAsync(symbol);
        }

        public async Task<RestResponse> GetFundingRateAsync(string symbol)
        {
            return await _bitunixClient.GetFundingRateAsync(symbol);
        }
        
        public async Task<decimal> GetLivePrice(string symbol)
        {
            var response = await GetTickerAsync(symbol);
            // Parse the response to get the live price
            return ParsePriceFromResponse(response.Content);
        }

        public decimal GetMovingAverage(string symbol, string period)
        {
            var response = GetKlineDataAsync(symbol, "1m", period).Result;
            // Calculate the moving average from the response

            var lim = int.TryParse(period, out int per) ? per : 0;
            return CalculateMovingAverage(response.Content, lim);
        }

        public decimal GetOrderBookSpread(string symbol)
        {
            var response = GetOrderBookAsync(symbol, "5").Result;
            // Calculate the order book spread from the response
            return CalculateOrderBookSpread(response.Content);
        }

        public (decimal bidVolume, decimal askVolume) GetOrderBookVolumes(string symbol)
        {
            var response = GetOrderBookAsync(symbol, "4").Result;
            // Parse the response to get the bid and ask volumes
            return ParseOrderBookVolumes(response.Content);
        }

        private decimal ParsePriceFromResponse(string response)
        {
            // Implement the logic to parse the price from the response
            // This is a placeholder implementation
            return 0m;
        }

        private decimal CalculateMovingAverage(string response, int period)
        {
            // Implement the logic to calculate the moving average from the response
            // This is a placeholder implementation
            return 0m;
        }

        private decimal CalculateOrderBookSpread(string response)
        {
            // Implement the logic to calculate the order book spread from the response
            // This is a placeholder implementation
            return 0m;
        }

        private (decimal bidVolume, decimal askVolume) ParseOrderBookVolumes(string response)
        {
            // Implement the logic to parse the bid and ask volumes from the response
            // This is a placeholder implementation
            return (0m, 0m);
        }

        public Task<DateTime> GetServerTimeAsync()
        {
            throw new NotImplementedException();
        }

        Task<string> IMarketData.GetOrderBookAsync(string symbol, string limit)
        {
            throw new NotImplementedException();
        }

        Task<string> IMarketData.GetKlineDataAsync(string symbol, string interval, string limit)
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

        Task<string> IMarketData.GetFundingRateAsync(string symbol)
        {
            throw new NotImplementedException();
        }

        public Task<string> GetRecentTradesAsync(string? symbol = null, long? startTime = null, long? endTime = null, int? limit = null)
        {
            throw new NotImplementedException();
        }

        decimal IMarketData.GetLivePrice(string symbol)
        {
            throw new NotImplementedException();
        }
    }
}