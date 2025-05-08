using Newtonsoft.Json;

namespace TidyTrader.ApiIntegration.Models.Responses.TpSl
{
    public class ModifyTpSlOrderResult
    {
        [JsonProperty("orderId")]
        public string OrderId { get; set; }
    }

    public class ModifyTpSlOrderResponse
    {
        [JsonProperty("code")]
        public int Code { get; set; }

        [JsonProperty("data")]
        public ModifyTpSlOrderResult Data { get; set; }

        [JsonProperty("msg")]
        public string Message { get; set; }
    }

}
