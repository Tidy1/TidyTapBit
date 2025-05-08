using Newtonsoft.Json;

namespace TidyTrader.ApiIntegration.Models.Responses.Trade
{
    public class CloseAllPositionResponse
    {
        [JsonProperty("code")]
        public int Code { get; set; }

        [JsonProperty("data")]
        public string Data { get; set; } // usually empty string

        [JsonProperty("msg")]
        public string Message { get; set; }
    }

}
