using RestSharp;

namespace TidyTrader.ApiIntegration.Interfaces
{
    public interface IBitunixApiClient
    {
        Task<string> CancelAllOrdersAsync(string symbol); // New method
        Task<string> CancelOrdersAsync(string symbol, string[] orderIds); // Ne
        Task<string> CancelTpSlOrderAsync(string symbol, string orderId);
        Task<RestResponse> GetAccountInfoAsync(string marginCoin);
        Task<string> GetFundingRateAsync(string symbol);
        Task<string> GetHistoryPositionsAsync(string symbol, int limit);
        Task<string> GetHistoryTpSlOrderAsync(string symbol);
        Task<string> GetHistoryTradesAsync(string? symbol = null, long? startTime = null, long? endTime = null, int? limit = null);
        Task<string> GetKlineAsync(string symbol, string interval, int limit);
        Task<string> GetMarketDepthAsync(string symbol, string limit);
        Task<string> GetMarketTickersAsync(string? symbol = null);
        Task<string> GetPendingPositionsAsync(string symbol);
        Task<string> GetPendingTpSlOrderAsync(string symbol);
        Task<string> GetPositionTiersAsync(string symbol);
        Task<string> GetServerTimeAsync();
        Task<string> GetTradingPairsAsync();
        Task<string> ModifyPositionTpSlOrderAsync(string symbol, string orderId, decimal stopLoss, decimal takeProfit);
        Task<string> ModifyTpSlOrderAsync(string symbol, string orderId, decimal stopLoss, decimal takeProfit);
        Task<string> PlaceFuturesOrderAsync(string symbol, decimal qty, string side, string orderType, int leverage, decimal? price = null, string clientOrderId = null, string timeInForce = null, bool? reduceOnly = null, bool? postOnly = null, string positionSide = null);
        Task<string> PlacePositionTpSlOrderAsync(string symbol, decimal stopLoss, decimal takeProfit);
        Task<string> PlaceTpSlOrderAsync(string symbol, decimal stopLoss, decimal takeProfit);
    }
}
