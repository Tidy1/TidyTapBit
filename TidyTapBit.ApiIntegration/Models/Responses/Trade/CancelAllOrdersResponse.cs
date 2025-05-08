using Newtonsoft.Json;

namespace TidyTrader.ApiIntegration.Models.Responses.Trade
{
    public class CancelOrderSuccess
    {
        [JsonProperty("orderId")]
        public string OrderId { get; set; }

        [JsonProperty("clientId")]
        public string ClientId { get; set; }
    }

    public class CancelOrderFailure
    {
        [JsonProperty("orderId")]
        public string OrderId { get; set; }

        [JsonProperty("clientId")]
        public string ClientId { get; set; }

        [JsonProperty("errorMsg")]
        public string ErrorMessage { get; set; }

        [JsonProperty("errorCode")]
        public string ErrorCode { get; set; }
    }

    public class CancelAllOrdersData
    {
        [JsonProperty("successList")]
        public List<CancelOrderSuccess> SuccessList { get; set; }

        [JsonProperty("failureList")]
        public List<CancelOrderFailure> FailureList { get; set; }
    }

    public class CancelAllOrdersResponse
    {
        [JsonProperty("code")]
        public int Code { get; set; }

        [JsonProperty("data")]
        public CancelAllOrdersData Data { get; set; }

        [JsonProperty("msg")]
        public string Message { get; set; }
    }

}
