using TidyTrader.ApiIntegration.Models;
using TidyTrader.ApiIntegration.Models.Responses.Market;
using TidyTrader.ApiIntegration.Models.Responses.Trade;

namespace TidyTrader.ApiIntegration.Interfaces
{
    public interface IMarketData
    {
        Task<string> DetectCandlePatternAsync(string symbol, string interval = "1m");
        Task<(decimal UpperBand, decimal LowerBand)> GetBollingerBandsAsync(string symbol, string interval = "1m", int period = 20, decimal stdDevMultiplier = 2);
        Task<string> GetExchangeInfoAsync();
        Task<FundingRateResponse> GetFundingRateAsync(string symbol);
        Task<KlineResponse> GetKlineDataAsync(string symbol, string interval, string limit);
        LeverageConfig GetLeverageConfig();
        Task<decimal> GetLivePriceAsync(string symbol);
        Task<MarketMetrics> GetMarketMetricsAsync(string symbol, string interval = "1m", int maPeriod = 20);
        Task<decimal> GetMovingAverageAsync(string symbol, string interval, int period);
        Task<MarketDepthResponse> GetOrderBookAsync(string symbol, string limit);
        Task<decimal> GetOrderBookSpreadAsync(string symbol);
        Task<(decimal bidVolume, decimal askVolume)> GetOrderBookVolumesAsync(string symbol);
        Task<decimal> GetPriceMomentumAsync(string symbol, string interval = "1m", int period = 10);
        Task<GetTradeHistoryResponse> GetRecentTradesAsync(string? symbol = null, long? startTime = null, long? endTime = null, int? limit = null);
        Task<decimal> GetRsiAsync(string symbol, string interval = "1m", int period = 14);
        Task<decimal> GetSmaAsync(string symbol, string interval = "1m", int period = 20);
        Task<TickerResponse> GetTickerAsync(string symbol);
        Task<decimal> GetVwapAsync(string symbol, string interval = "1m");
        void SetLeverageConfig(int minLeverage, int maxLeverage);
    }
}