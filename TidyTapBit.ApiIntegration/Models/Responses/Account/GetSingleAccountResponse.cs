using Newtonsoft.Json;

namespace TidyTrader.ApiIntegration.Models.Responses.Account
{
    public class AccountInfo
    {
        [JsonProperty("marginCoin")]
        public string MarginCoin { get; set; }

        [JsonProperty("available")]
        public string Available { get; set; }

        [JsonProperty("frozen")]
        public string Frozen { get; set; }

        [JsonProperty("margin")]
        public string Margin { get; set; }

        [JsonProperty("transfer")]
        public string Transfer { get; set; }

        [JsonProperty("positionMode")]
        public string PositionMode { get; set; }

        [JsonProperty("crossUnrealizedPNL")]
        public string CrossUnrealizedPnl { get; set; }

        [JsonProperty("isolationUnrealizedPNL")]
        public string IsolationUnrealizedPnl { get; set; }

        [JsonProperty("bonus")]
        public string Bonus { get; set; }
    }

    public class GetSingleAccountResponse
    {
        [JsonProperty("code")]
        public int Code { get; set; }

        [JsonProperty("data")]
        public AccountInfo Data { get; set; }

        [JsonProperty("msg")]
        public string Message { get; set; }
    }

}
