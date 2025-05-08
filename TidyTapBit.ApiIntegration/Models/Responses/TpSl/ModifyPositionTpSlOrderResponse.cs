using Newtonsoft.Json;

namespace TidyTrader.ApiIntegration.Models.Responses.TpSl
{
    public class ModifyPositionTpSlOrderResult
    {
        [JsonProperty("orderId")]
        public string OrderId { get; set; }
    }

    public class ModifyPositionTpSlOrderResponse
    {
        [JsonProperty("code")]
        public int Code { get; set; }

        [JsonProperty("data")]
        public ModifyPositionTpSlOrderResult Data { get; set; }

        [JsonProperty("msg")]
        public string Message { get; set; }
    }

}
