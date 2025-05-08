using Newtonsoft.Json;

namespace TidyTrader.ApiIntegration.Models.Responses.Trade
{
    public class PlaceOrderResult
    {
        [JsonProperty("orderId")]
        public string OrderId { get; set; }

        [JsonProperty("clientId")]
        public string ClientId { get; set; }
    }

    public class PlaceOrderResponse
    {
        [JsonProperty("code")]
        public int Code { get; set; }

        [JsonProperty("data")]
        public PlaceOrderResult Data { get; set; }

        [JsonProperty("msg")]
        public string Message { get; set; }
    }


}
