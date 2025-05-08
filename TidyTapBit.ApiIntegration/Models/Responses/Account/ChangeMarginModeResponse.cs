using Newtonsoft.Json;

namespace TidyTrader.ApiIntegration.Models.Responses.Account
{
    public class ChangeMarginModeResponse
    {
        [JsonProperty("code")]
        public int Code { get; set; }

        [JsonProperty("data")]
        public List<ChangeMarginModeItem> Data { get; set; }

        [JsonProperty("msg")]
        public string Message { get; set; }
    }

    public class ChangeMarginModeItem
    {
        [JsonProperty("positionMode")]
        public string PositionMode { get; set; } // Should this be MarginMode? Using response sample literally
    }
}
