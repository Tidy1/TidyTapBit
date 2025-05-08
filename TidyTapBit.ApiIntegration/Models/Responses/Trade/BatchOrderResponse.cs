using Newtonsoft.Json;

namespace TidyTrader.ApiIntegration.Models.Responses.Trade
{
    public class BatchOrderSuccess
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("clientId")]
        public string ClientId { get; set; }
    }

    public class BatchOrderFailure
    {
        [JsonProperty("clientId")]
        public string ClientId { get; set; }

        [JsonProperty("errorMsg")]
        public string ErrorMessage { get; set; }

        [JsonProperty("errorCode")]
        public string ErrorCode { get; set; }
    }

    public class BatchOrderResult
    {
        [JsonProperty("successList")]
        public List<BatchOrderSuccess> SuccessList { get; set; }

        [JsonProperty("failureList")]
        public List<BatchOrderFailure> FailureList { get; set; }
    }

    public class BatchOrderResponse
    {
        [JsonProperty("code")]
        public int Code { get; set; }

        [JsonProperty("data")]
        public BatchOrderResult Data { get; set; }

        [JsonProperty("msg")]
        public string Message { get; set; }
    }

}
