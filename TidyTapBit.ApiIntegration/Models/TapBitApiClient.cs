using RestSharp;

using TidyTapBit.Core.Interfaces;
using TidyTapBit.Core.Models;

namespace TidyTapBit.ApiIntegration.Models
{
    public class TapBitApiClient : ITradingApiClient
    {
        private readonly RestClient _client;
        private string apiId = "1208f4a4096c834df9ee2b6c73482490";
        private string apiSecret = "237c6796641c4fd5860b546f032ab43b";

        public TapBitApiClient(string baseUrl, string apiKey, string apiSecret)
        {
            _client = new RestClient(baseUrl);
            _client.AddDefaultHeader("Api-Key", apiKey);
            _client.AddDefaultHeader("Api-Secret", apiSecret);
        }

        public async Task<string> PlaceSpotOrderAsync(SpotTradeOrder order)
        {
            var request = new RestRequest("/v1/spot/order/place", Method.Post);
            request.AddJsonBody(new
            {
                symbol = order.Symbol,
                side = order.Side,
                quantity = order.Quantity,
                price = order.Price,
                orderType = order.OrderType,
                marginAmount = order.MarginAmount
            });

            var response = await _client.ExecuteAsync(request);
            return response.Content;
        }

        public async Task<string> PlacePerpetualOrderAsync(PerpetualTradeOrder order)
        {
            var request = new RestRequest("/v1/perpetual/order/place", Method.Post);
            request.AddJsonBody(new
            {
                symbol = order.Symbol,
                side = order.Side,
                quantity = order.Quantity,
                price = order.Price,
                orderType = order.OrderType,
                leverage = order.Leverage,
                margin_mode = order.MarginMode
            });

            var response = await _client.ExecuteAsync(request);
            return response.Content;
        }

        public async Task<string> CancelOrderAsync(string orderId)
        {
            var request = new RestRequest("/v1/order/cancel", Method.Post);
            request.AddJsonBody(new { orderId });

            var response = await _client.ExecuteAsync(request);
            return response.Content;
        }

        public async Task<string> GetOrderStatusAsync(string orderId)
        {
            var request = new RestRequest($"/v1/order/status", Method.Get);
            request.AddParameter("orderId", orderId);

            var response = await _client.ExecuteAsync(request);
            return response.Content;
        }
    }
}
