using RestSharp;

using TidyTrader.ApiIntegration.Models;

namespace TidyTrader.ApiIntegration.Interfaces
{
    public interface IMarketData
    {
        Task<string> GetExchangeInfoAsync();
        Task<string> GetFundingRateAsync(string symbol);
        Task<string> GetKlineDataAsync(string symbol, string interval, string limit);
        LeverageConfig GetLeverageConfig();
        Task<decimal> GetLivePriceAsync(string symbol);
        Task<decimal> GetMovingAverageAsync(string symbol, string interval, int period);
        Task<string> GetOrderBookAsync(string symbol, string limit);
        Task<decimal> GetOrderBookSpreadAsync(string symbol);
        Task<(decimal bidVolume, decimal askVolume)> GetOrderBookVolumesAsync(string symbol);
        Task<string> GetRecentTradesAsync(string? symbol = null, long? startTime = null, long? endTime = null, int? limit = null);
        Task<DateTime> GetServerTimeAsync();
        Task<string> GetTickerAsync(string symbol);
        Task<string> GetTickerListAsync();
        Task<RestResponse> PlaceIsolatedTradeAsync(string symbol, string qty, string side, string orderType);
        void SetLeverageConfig(int minLeverage, int maxLeverage);
    }
}