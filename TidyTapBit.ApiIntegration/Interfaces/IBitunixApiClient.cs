using RestSharp;

namespace TidyTrader.ApiIntegration.Models
{
    public interface IBitunixApiClient
    {
        Task<RestResponse> AdjustPositionMarginAsync(string marginCoin, string symbol, decimal amount, int type);
        Task<RestResponse> CancelAllOrdersAsync(string symbol);
        Task<RestResponse> CancelOrdersAsync(string symbol, IEnumerable<string> orderIds);
        Task<RestResponse> CancelTpSlOrderAsync(string symbol, string orderId);
        Task<RestResponse> ChangeLeverageAsync(string symbol, string marginCoin, int leverage);
        Task<RestResponse> ChangeMarginModeAsync(string symbol, string marginCoin, string marginMode);
        Task<RestResponse> ChangePositionModeAsync(string symbol, string marginCoin, int positionMode);
        Task<RestResponse> CloseAllPositionsAsync(string symbol);
        Task<RestResponse> FlashClosePositionAsync(string positionId);
        Task<RestResponse> GetAccountInfoAsync(string marginCoin);
        Task<RestResponse> GetFundingRateAsync(string symbol);
        Task<RestResponse> GetHistoryOrdersAsync(string symbol = null, string orderId = null, string clientId = null, string status = null, string type = null, long? startTime = null, long? endTime = null, long? skip = null, long? limit = null);
        Task<RestResponse> GetHistoryPositionsAsync(string symbol = null, string positionId = null, long? startTime = null, long? endTime = null, long? skip = null, long? limit = null);
        Task<RestResponse> GetHistoryTradesAsync(string symbol = null, string orderId = null, string positionId = null, long? startTime = null, long? endTime = null, long? skip = null, long? limit = null);
        Task<RestResponse> GetKlineAsync(string symbol, string interval, long? startTime = null, long? endTime = null, int? limit = null, string type = null);
        Task<RestResponse> GetLeverageAndMarginModeAsync(string symbol, string marginCoin);
        Task<RestResponse> GetMarketDepthAsync(string symbol, string limit = null);
        Task<RestResponse> GetOrderDetailAsync(string orderId = null, string clientId = null);
        Task<RestResponse> GetPendingOrdersAsync(string symbol = null, string orderId = null, string clientId = null, string status = null, long? startTime = null, long? endTime = null, long? skip = null, long? limit = null);
        Task<RestResponse> GetPendingPositionsAsync(string symbol = null, string positionId = null);
        Task<RestResponse> GetPendingTpSlOrdersAsync(string symbol = null, string positionId = null, int? side = null, int? positionMode = null, long? skip = null, long? limit = null);
        Task<RestResponse> GetPositionTiersAsync(string symbol);
        Task<RestResponse> GetSingleAccountAsync(string symbol, string marginCoin);
        Task<RestResponse> GetTickersAsync(string symbols = null);
        Task<RestResponse> GetTpSlOrderHistoryAsync(string symbol = null, int? side = null, int? positionMode = null, long? startTime = null, long? endTime = null, long? skip = null, long? limit = null);
        Task<RestResponse> GetTradingPairsAsync(string symbols = null);
        Task<RestResponse> ModifyOrderAsync(string qty, string price, string orderId = null, string clientId = null, string tpPrice = null, string tpStopType = null, string tpOrderType = null, string tpOrderPrice = null, string slPrice = null, string slStopType = null, string slOrderType = null, string slOrderPrice = null);
        Task<RestResponse> ModifyPositionTpSlOrderAsync(string symbol, string positionId, string tpPrice = null, string tpStopType = null, string slPrice = null, string slStopType = null);
        Task<RestResponse> ModifyTpSlOrderAsync(string orderId, string tpPrice = null, string tpStopType = null, string tpOrderType = null, string tpOrderPrice = null, string tpQty = null, string slPrice = null, string slStopType = null, string slOrderType = null, string slOrderPrice = null, string slQty = null);
        Task<RestResponse> PlaceBatchOrdersAsync(string symbol, string marginCoin, IEnumerable<object> orderDataList);
        Task<RestResponse> PlaceOrderAsync(string symbol, string qty, string side, string tradeSide, string orderType, string price = null, string effect = null, string clientId = null, string positionId = null, bool? reduceOnly = null, string tpPrice = null, string tpStopType = null, string tpOrderType = null, string tpOrderPrice = null, string slPrice = null, string slStopType = null, string slOrderType = null, string slOrderPrice = null);
        Task<RestResponse> PlacePositionTpSlOrderAsync(string symbol, string positionId, string tpPrice = null, string tpStopType = null, string slPrice = null, string slStopType = null);
        Task<RestResponse> PlaceTpSlOrderAsync(string symbol, string positionId, string tpPrice = null, string tpStopType = null, string tpOrderType = null, string tpOrderPrice = null, string tpQty = null, string slPrice = null, string slStopType = null, string slOrderType = null, string slOrderPrice = null, string slQty = null);
    }
}