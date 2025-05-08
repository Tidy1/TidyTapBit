using Newtonsoft.Json;

namespace TidyTrader.ApiIntegration.Models.Responses.Market
{
    public class PendingPositionItem
    {
        [JsonProperty("positionId")]
        public string PositionId { get; set; }

        [JsonProperty("symbol")]
        public string Symbol { get; set; }

        [JsonProperty("qty")]
        public string Qty { get; set; }

        [JsonProperty("entryValue")]
        public string EntryValue { get; set; }

        [JsonProperty("side")]
        public string Side { get; set; }

        [JsonProperty("marginMode")]
        public string MarginMode { get; set; }

        [JsonProperty("positionMode")]
        public string PositionMode { get; set; }

        [JsonProperty("leverage")]
        public int Leverage { get; set; }

        [JsonProperty("fee")]
        public string Fee { get; set; }

        [JsonProperty("funding")]
        public string Funding { get; set; }

        [JsonProperty("realizedPNL")]
        public string RealizedPNL { get; set; }

        [JsonProperty("margin")]
        public string Margin { get; set; }

        [JsonProperty("unrealizedPNL")]
        public string UnrealizedPNL { get; set; }

        [JsonProperty("liqPrice")]
        public string LiqPrice { get; set; }

        [JsonProperty("marginRate")]
        public string MarginRate { get; set; }

        [JsonProperty("avgOpenPrice")]
        public string AvgOpenPrice { get; set; }

        [JsonProperty("ctime")]
        public long CTime { get; set; }

        [JsonProperty("mtime")]
        public long MTime { get; set; }
    }

    public class PendingPositionsResponse
    {
        [JsonProperty("data")]
        public List<PendingPositionItem> Data { get; set; }
    }

}
