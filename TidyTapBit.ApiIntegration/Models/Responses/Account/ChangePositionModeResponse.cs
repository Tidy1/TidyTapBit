using Newtonsoft.Json;

namespace TidyTrader.ApiIntegration.Models.Responses.Account
{
    public class ChangePositionModeResponse
    {
        [JsonProperty("code")]
        public int Code { get; set; }

        [JsonProperty("data")]
        public List<ChangePositionModeItem> Data { get; set; }

        [JsonProperty("msg")]
        public string Message { get; set; }
    }

    public class ChangePositionModeItem
    {
        [JsonProperty("positionMode")]
        public string PositionMode { get; set; }
    }
}
