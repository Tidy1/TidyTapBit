using Newtonsoft.Json;

namespace TidyTrader.ApiIntegration.Models.Responses.Market
{
    public class PositionItem
    {
        [JsonProperty("positionId")]
        public string PositionId { get; set; }

        [JsonProperty("symbol")]
        public string Symbol { get; set; }

        [JsonProperty("maxQty")]
        public string MaxQty { get; set; }

        [JsonProperty("entryPrice")]
        public string EntryPrice { get; set; }

        [JsonProperty("closePrice")]
        public string ClosePrice { get; set; }

        [JsonProperty("liqQty")]
        public string LiqQty { get; set; }

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

        [JsonProperty("liqPrice")]
        public string LiqPrice { get; set; }

        [JsonProperty("ctime")]
        public long CTime { get; set; }

        [JsonProperty("mtime")]
        public long MTime { get; set; }
    }

    public class HistoryPositionsResponse
    {
        [JsonProperty("positionList")]
        public List<PositionItem> PositionList { get; set; }

        [JsonProperty("total")]
        public long Total { get; set; }
    }


}
