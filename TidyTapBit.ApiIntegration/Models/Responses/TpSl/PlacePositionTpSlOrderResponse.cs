using Newtonsoft.Json;

namespace TidyTrader.ApiIntegration.Models.Responses.TpSl
{
    public class PlacePositionTpSlOrderResult
    {
        [JsonProperty("orderId")]
        public string OrderId { get; set; }
    }

    public class PlacePositionTpSlOrderResponse
    {
        [JsonProperty("code")]
        public int Code { get; set; }

        [JsonProperty("data")]
        public PlacePositionTpSlOrderResult Data { get; set; }

        [JsonProperty("msg")]
        public string Message { get; set; }
    }

}
