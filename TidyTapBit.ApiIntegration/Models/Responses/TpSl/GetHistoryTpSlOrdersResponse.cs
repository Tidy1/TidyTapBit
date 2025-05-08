using Newtonsoft.Json;

namespace TidyTrader.ApiIntegration.Models.Responses.TpSl
{
    public class TpSlOrderItem
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("positionId")]
        public string PositionId { get; set; }

        [JsonProperty("symbol")]
        public string Symbol { get; set; }

        [JsonProperty("base")]
        public string Base { get; set; }

        [JsonProperty("quote")]
        public string Quote { get; set; }

        [JsonProperty("tpPrice")]
        public string TpPrice { get; set; }

        [JsonProperty("tpStopType")]
        public string TpStopType { get; set; }

        [JsonProperty("slPrice")]
        public string SlPrice { get; set; }

        [JsonProperty("slStopType")]
        public string SlStopType { get; set; }

        [JsonProperty("tpOrderType")]
        public string TpOrderType { get; set; }

        [JsonProperty("tpOrderPrice")]
        public string TpOrderPrice { get; set; }

        [JsonProperty("slOrderType")]
        public string SlOrderType { get; set; }

        [JsonProperty("slOrderPrice")]
        public string SlOrderPrice { get; set; }

        [JsonProperty("tpQty")]
        public string TpQty { get; set; }

        [JsonProperty("slQty")]
        public string SlQty { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("ctime")]
        public long CTime { get; set; }

        [JsonProperty("triggerTime")]
        public long TriggerTime { get; set; }
    }

    public class GetHistoryTpSlOrdersResponse
    {
        [JsonProperty("code")]
        public int Code { get; set; }

        [JsonProperty("data")]
        public List<TpSlOrderItem> Data { get; set; }

        [JsonProperty("msg")]
        public string Message { get; set; }
    }

}
