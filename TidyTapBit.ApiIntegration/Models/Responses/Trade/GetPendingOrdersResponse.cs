using Newtonsoft.Json;

namespace TidyTrader.ApiIntegration.Models.Responses.Trade
{
    public class PendingOrdersData
    {
        [JsonProperty("orderList")]
        public List<OrderHistoryItem> OrderList { get; set; }

        [JsonProperty("total")]
        public long Total { get; set; }
    }

    public class GetPendingOrdersResponse
    {
        [JsonProperty("code")]
        public int Code { get; set; }

        [JsonProperty("data")]
        public PendingOrdersData Data { get; set; }

        [JsonProperty("msg")]
        public string Message { get; set; }
    }

}
