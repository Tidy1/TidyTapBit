using TidyTrader.ApiIntegration.Models;
using TidyTrader.ApiIntegration.Models.Responses.Account;
using TidyTrader.ApiIntegration.Models.Responses.Market;
using TidyTrader.ApiIntegration.Models.Responses.Position;
using TidyTrader.ApiIntegration.Models.Responses.TpSl;
using TidyTrader.ApiIntegration.Models.Responses.Trade;

namespace TidyTrader.ApiIntegration.Interfaces
{
    public interface IBitunixApiClient
    {
        Task<ApiResponse<AdjustPositionMarginResponse>> AdjustPositionMarginAsync(string marginCoin, string symbol, decimal amount, string side = null, string positionId = null);
        Task<ApiResponse<BatchOrderResponse>> BatchOrdersAsync(string symbol, string marginCoin, IEnumerable<object> orderDataList);
        Task<ApiResponse<CancelAllOrdersResponse>> CancelAllOrdersAsync(string symbol);
        Task<ApiResponse<CancelOrdersResponse>> CancelOrdersAsync(string symbol, IEnumerable<string> orderIds);
        Task<ApiResponse<CancelTpSlOrderResponse>> CancelTpSlOrderAsync(string symbol, string orderId);
        Task<ApiResponse<ChangeLeverageResponse>> ChangeLeverageAsync(string symbol, string marginCoin, int leverage);
        Task<ApiResponse<ChangeMarginModeResponse>> ChangeMarginModeAsync(string symbol, string marginCoin, string marginMode);
        Task<ApiResponse<ChangePositionModeResponse>> ChangePositionModeAsync(string symbol, string marginCoin, int positionMode);
        Task<ApiResponse<CloseAllPositionResponse>> CloseAllPositionsAsync(string symbol);
        Task<ApiResponse<FlashClosePositionResponse>> FlashClosePositionAsync(string positionId);
        Task<ApiResponse<FundingRateResponse>> GetFundingRateAsync(string symbol);
        Task<ApiResponse<GetOrderHistoryResponse>> GetHistoryOrdersAsync(string symbol = null, string orderId = null, string clientId = null, string status = null, string type = null, long? startTime = null, long? endTime = null, long? skip = null, long? limit = null);
        Task<ApiResponse<HistoryPositionsResponse>> GetHistoryPositionsAsync(string symbol = null, string positionId = null, long? startTime = null, long? endTime = null, long? skip = null, long? limit = null);
        Task<ApiResponse<GetTradeHistoryResponse>> GetHistoryTradesAsync(string symbol = null, string orderId = null, string positionId = null, long? startTime = null, long? endTime = null, long? skip = null, long? limit = null);
        Task<ApiResponse<KlineResponse>> GetKlineAsync(string symbol, string interval, long? startTime = null, long? endTime = null, int? limit = null, string type = null);
        Task<ApiResponse<GetLeverageMarginModeResponse>> GetLeverageAndMarginModeAsync(string symbol, string marginCoin);
        Task<ApiResponse<MarketDepthResponse>> GetMarketDepthAsync(string symbol, string limit = null);
        Task<ApiResponse<GetOrderDetailResponse>> GetOrderDetailAsync(string orderId = null, string clientId = null);
        Task<ApiResponse<GetPendingOrdersResponse>> GetPendingOrdersAsync(string symbol = null, string orderId = null, string clientId = null, string status = null, long? startTime = null, long? endTime = null, long? skip = null, long? limit = null);
        Task<ApiResponse<PendingPositionsResponse>> GetPendingPositionsAsync(string symbol = null, string positionId = null);
        Task<ApiResponse<GetPendingTpSlOrdersResponse>> GetPendingTpSlOrdersAsync(string symbol = null, string positionId = null, int? side = null, int? positionMode = null, long? skip = null, long? limit = null);
        Task<ApiResponse<PositionTiersResponse>> GetPositionTiersAsync(string symbol);
        Task<ApiResponse<GetSingleAccountResponse>> GetSingleAccountAsync(string marginCoin);
        Task<ApiResponse<TickerResponse>> GetTickersAsync(string symbols = null);
        Task<ApiResponse<GetHistoryTpSlOrdersResponse>> GetTpSlOrderHistoryAsync(string symbol = null, int? side = null, int? positionMode = null, long? startTime = null, long? endTime = null, long? skip = null, long? limit = null);
        Task<ApiResponse<TradingPairsResponse>> GetTradingPairsAsync(string symbols = null);
        Task<ApiResponse<ModifyOrderResponse>> ModifyOrderAsync(string qty, string price, string orderId = null, string clientId = null, string tpPrice = null, string tpStopType = null, string tpOrderType = null, string tpOrderPrice = null, string slPrice = null, string slStopType = null, string slOrderType = null, string slOrderPrice = null);
        Task<ApiResponse<ModifyPositionTpSlOrderResponse>> ModifyPositionTpSlOrderAsync(string symbol, string positionId, string tpPrice = null, string tpStopType = null, string slPrice = null, string slStopType = null);
        Task<ApiResponse<ModifyTpSlOrderResponse>> ModifyTpSlOrderAsync(string orderId, string tpPrice = null, string tpStopType = null, string tpOrderType = null, string tpOrderPrice = null, string tpQty = null, string slPrice = null, string slStopType = null, string slOrderType = null, string slOrderPrice = null, string slQty = null);
        Task<ApiResponse<PlaceOrderResponse>> PlaceOrderAsync(string symbol, string qty, string side, string tradeSide, string orderType, string price = null, string effect = null, string clientId = null, string positionId = null, bool? reduceOnly = null, string tpPrice = null, string tpStopType = null, string tpOrderType = null, string tpOrderPrice = null, string slPrice = null, string slStopType = null, string slOrderType = null, string slOrderPrice = null);
        Task<ApiResponse<PlacePositionTpSlOrderResponse>> PlacePositionTpSlOrderAsync(string symbol, string positionId, string tpPrice = null, string tpStopType = null, string slPrice = null, string slStopType = null);
        Task<ApiResponse<PlaceTpSlOrderResponse>> PlaceTpSlOrderAsync(string symbol, string positionId, string tpPrice = null, string tpStopType = null, string tpOrderType = null, string tpOrderPrice = null, string tpQty = null, string slPrice = null, string slStopType = null, string slOrderType = null, string slOrderPrice = null, string slQty = null);
    }
}