using Newtonsoft.Json;

namespace TidyTrader.ApiIntegration.Models.Responses.TpSl
{
    public class CancelTpSlOrderResult
    {
        [JsonProperty("orderId")]
        public string OrderId { get; set; }
    }

    public class CancelTpSlOrderResponse
    {
        [JsonProperty("code")]
        public int Code { get; set; }

        [JsonProperty("data")]
        public CancelTpSlOrderResult Data { get; set; }

        [JsonProperty("msg")]
        public string Message { get; set; }
    }

}
