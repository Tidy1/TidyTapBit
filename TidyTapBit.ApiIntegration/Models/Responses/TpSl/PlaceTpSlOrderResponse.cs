using Newtonsoft.Json;

namespace TidyTrader.ApiIntegration.Models.Responses.TpSl
{
    public class PlaceTpSlOrderResult
    {
        [JsonProperty("orderId")]
        public string OrderId { get; set; }
    }

    public class PlaceTpSlOrderResponse
    {
        [JsonProperty("code")]
        public int Code { get; set; }

        [JsonProperty("data")]
        public PlaceTpSlOrderResult Data { get; set; }

        [JsonProperty("msg")]
        public string Message { get; set; }
    }

}
