using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TidyTapBit.ApiIntegration.Interfaces
{
    public interface IMarketData
    {
        Task<DateTime> GetServerTimeAsync();
        Task<string> GetExchangeInfoAsync();
        Task<string> GetOrderBookAsync(string symbol, string limit);
        Task<string> GetKlineDataAsync(string symbol, string interval, string limit);
        Task<string> GetTickerAsync(string symbol);
        Task<string> GetTickerListAsync();
        Task<string> GetFundingRateAsync(string symbol);
        Task<string> GetRecentTradesAsync(string? symbol = null, long? startTime = null, long? endTime = null, int? limit = null);


        // Methods from IMarketDataProvider
        decimal GetLivePrice(string symbol);
        decimal GetMovingAverage(string symbol, string period);
        decimal GetOrderBookSpread(string symbol);
        (decimal bidVolume, decimal askVolume) GetOrderBookVolumes(string symbol);
    }
}