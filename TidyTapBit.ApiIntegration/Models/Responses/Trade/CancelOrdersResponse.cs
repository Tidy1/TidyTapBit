using Newtonsoft.Json;

namespace TidyTrader.ApiIntegration.Models.Responses.Trade
{
    public class CancelOrdersData
    {
        [JsonProperty("successList")]
        public List<CancelOrderSuccess> SuccessList { get; set; }

        [JsonProperty("failureList")]
        public List<CancelOrderFailure> FailureList { get; set; }
    }

    public class CancelOrdersResponse
    {
        [JsonProperty("code")]
        public int Code { get; set; }

        [JsonProperty("data")]
        public CancelOrdersData Data { get; set; }

        [JsonProperty("msg")]
        public string Message { get; set; }
    }
}
