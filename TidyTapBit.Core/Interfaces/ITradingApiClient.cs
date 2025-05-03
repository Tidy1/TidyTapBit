using TidyTrader.Core.Models;

namespace TidyTrader.Core.Interfaces
{
    public interface ITradingApiClient
    {
        Task<string> PlaceSpotOrderAsync(SpotTradeOrder order);
        Task<string> PlacePerpetualOrderAsync(PerpetualTradeOrder order);
        Task<string> CancelOrderAsync(string orderId);
        Task<string> GetOrderStatusAsync(string orderId);
    }
}
