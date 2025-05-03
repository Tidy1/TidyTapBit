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

        public async Task<string> PlaceIsolatedTradeAsync(string symbol, decimal qty, string side, string orderType)
        {
            int leverage = DetermineLeverage();
            return await _bitunixClient.PlaceFuturesOrderAsync(symbol, qty, side, orderType, leverage);
        }

        public async Task<DateTime> GetServerTimeAsync()
        {
            var response = await _bitunixClient.GetServerTimeAsync();
            // Parse the response to get the server time
            return DateTime.Parse(response);
        }

        public async Task<string> GetExchangeInfoAsync()
        {
            return "none";
            //return await _bitunixClient.GetExchangeInfoAsync();
        }

        public async Task<string> GetOrderBookAsync(string symbol, string limit)
        {
            return await _bitunixClient.GetMarketDepthAsync(symbol, limit);
        }

        public async Task<string> GetKlineDataAsync(string symbol, string interval, string limit)
        {
            return await _bitunixClient.GetKlineAsync(symbol, interval, 6);
        }

        public async Task<string> GetTickerAsync(string? symbol)
        {
            return await _bitunixClient.GetMarketTickersAsync(symbol);
        }

        public async Task<string> GetTickerListAsync()
        {           
            return await _bitunixClient.GetMarketTickersAsync();
        }

        public async Task<string> GetFundingRateAsync(string symbol)
        {
            return await _bitunixClient.GetFundingRateAsync(symbol);
        }

        public async Task<string> GetRecentTradesAsync(string? symbol = null, long? startTime = null, long? endTime = null, int? limit = null)
        {            
            return await _bitunixClient.GetHistoryTradesAsync(symbol,startTime,endTime,limit);
        }

        public decimal GetLivePrice(string symbol)
        {
            var response = GetTickerAsync(symbol).Result;
            // Parse the response to get the live price
            return ParsePriceFromResponse(response);
        }

        public decimal GetMovingAverage(string symbol, string period)
        {
            var response = GetKlineDataAsync(symbol, "1m", period).Result;
            // Calculate the moving average from the response

            var lim = int.TryParse(period, out int per) ? per : 0;
            return CalculateMovingAverage(response, lim);
        }

        public decimal GetOrderBookSpread(string symbol)
        {
            var response = GetOrderBookAsync(symbol, "5").Result;
            // Calculate the order book spread from the response
            return CalculateOrderBookSpread(response);
        }

        public (decimal bidVolume, decimal askVolume) GetOrderBookVolumes(string symbol)
        {
            var response = GetOrderBookAsync(symbol, "4").Result;
            // Parse the response to get the bid and ask volumes
            return ParseOrderBookVolumes(response);
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
    }
}