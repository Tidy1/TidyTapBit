using Newtonsoft.Json;

namespace TidyTrader.ApiIntegration.Models.Responses.Trade
{
    public class TradeHistoryItem
    {
        [JsonProperty("tradeId")]
        public string TradeId { get; set; }

        [JsonProperty("orderId")]
        public string OrderId { get; set; }

        [JsonProperty("symbol")]
        public string Symbol { get; set; }

        [JsonProperty("qty")]
        public string Quantity { get; set; }

        [JsonProperty("price")]
        public string Price { get; set; }

        [JsonProperty("positionMode")]
        public string PositionMode { get; set; }

        [JsonProperty("marginMode")]
        public string MarginMode { get; set; }

        [JsonProperty("leverage")]
        public int Leverage { get; set; }

        [JsonProperty("fee")]
        public string Fee { get; set; }

        [JsonProperty("realizedPNL")]
        public string RealizedPnl { get; set; }

        [JsonProperty("type")]
        public string OrderType { get; set; }

        [JsonProperty("effect")]
        public string Effect { get; set; }

        [JsonProperty("reduceOnly")]
        public bool ReduceOnly { get; set; }

        [JsonProperty("clientId")]
        public string ClientId { get; set; }

        [JsonProperty("source")]
        public string Source { get; set; }

        [JsonProperty("ctime")]
        public long CreateTime { get; set; }

        [JsonProperty("roleType")]
        public string RoleType { get; set; }
    }

    public class TradeHistoryData
    {
        [JsonProperty("tradeList")]
        public List<TradeHistoryItem> TradeList { get; set; }

        [JsonProperty("total")]
        public long Total { get; set; }
    }

    public class GetTradeHistoryResponse
    {
        [JsonProperty("code")]
        public int Code { get; set; }

        [JsonProperty("data")]
        public TradeHistoryData Data { get; set; }

        [JsonProperty("msg")]
        public string Message { get; set; }
    }

}
