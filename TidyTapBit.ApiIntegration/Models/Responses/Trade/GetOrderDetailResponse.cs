using Newtonsoft.Json;

namespace TidyTrader.ApiIntegration.Models.Responses.Trade
{
    public class OrderDetailItem
    {
        [JsonProperty("orderId")]
        public string OrderId { get; set; }

        [JsonProperty("symbol")]
        public string Symbol { get; set; }

        [JsonProperty("qty")]
        public string Quantity { get; set; }

        [JsonProperty("tradeQty")]
        public string TradeQuantity { get; set; }

        [JsonProperty("positionMode")]
        public string PositionMode { get; set; }

        [JsonProperty("marginMode")]
        public string MarginMode { get; set; }

        [JsonProperty("leverage")]
        public int Leverage { get; set; }

        [JsonProperty("price")]
        public string Price { get; set; }

        [JsonProperty("side")]
        public string Side { get; set; }

        [JsonProperty("orderType")]
        public string OrderType { get; set; }

        [JsonProperty("effect")]
        public string Effect { get; set; }

        [JsonProperty("clientId")]
        public string ClientId { get; set; }

        [JsonProperty("reduceOnly")]
        public bool ReduceOnly { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("fee")]
        public string Fee { get; set; }

        [JsonProperty("realizedPNL")]
        public string RealizedPnl { get; set; }

        [JsonProperty("tpPrice")]
        public string TpPrice { get; set; }

        [JsonProperty("tpStopType")]
        public string TpStopType { get; set; }

        [JsonProperty("tpOrderType")]
        public string TpOrderType { get; set; }

        [JsonProperty("tpOrderPrice")]
        public string TpOrderPrice { get; set; }

        [JsonProperty("slPrice")]
        public string SlPrice { get; set; }

        [JsonProperty("slStopType")]
        public string SlStopType { get; set; }

        [JsonProperty("slOrderType")]
        public string SlOrderType { get; set; }

        [JsonProperty("slOrderPrice")]
        public string SlOrderPrice { get; set; }

        [JsonProperty("ctime")]
        public long CreateTime { get; set; }

        [JsonProperty("mtime")]
        public long ModifyTime { get; set; }
    }

    public class GetOrderDetailResponse
    {
        [JsonProperty("code")]
        public int Code { get; set; }

        [JsonProperty("data")]
        public OrderDetailItem Data { get; set; }

        [JsonProperty("msg")]
        public string Message { get; set; }
    }


}
