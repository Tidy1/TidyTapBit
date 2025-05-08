using Newtonsoft.Json;

namespace TidyTrader.ApiIntegration.Models.Responses.Account
{
    public class LeverageMarginModeData
    {
        [JsonProperty("symbol")]
        public string Symbol { get; set; }

        [JsonProperty("marginCoin")]
        public string MarginCoin { get; set; }

        [JsonProperty("leverage")]
        public int Leverage { get; set; }

        [JsonProperty("marginMode")]
        public string MarginMode { get; set; } // ISOLATION or CROSS
    }

    public class GetLeverageMarginModeResponse
    {
        [JsonProperty("code")]
        public int Code { get; set; }

        [JsonProperty("data")]
        public LeverageMarginModeData Data { get; set; }

        [JsonProperty("msg")]
        public string Message { get; set; }
    }

}
