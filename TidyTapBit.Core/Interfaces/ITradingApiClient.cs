using TidyTapBit.Core.Models;

namespace TidyTapBit.Core.Interfaces
{
    public interface ITradingApiClient
    {
        Task<string> PlaceSpotOrderAsync(SpotTradeOrder order);
        Task<string> PlacePerpetualOrderAsync(PerpetualTradeOrder order);
        Task<string> CancelOrderAsync(string orderId);
        Task<string> GetOrderStatusAsync(string orderId);
    }
}
