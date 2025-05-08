using Newtonsoft.Json;

namespace TidyTrader.ApiIntegration.Models.Responses.Trade
{
    public class ModifyOrderResult
    {
        [JsonProperty("orderId")]
        public string OrderId { get; set; }

        [JsonProperty("clientId")]
        public string ClientId { get; set; }
    }

    public class ModifyOrderResponse
    {
        [JsonProperty("code")]
        public int Code { get; set; }

        [JsonProperty("data")]
        public ModifyOrderResult Data { get; set; }

        [JsonProperty("msg")]
        public string Message { get; set; }
    }

}
