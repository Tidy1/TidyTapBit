using Newtonsoft.Json;

namespace TidyTrader.ApiIntegration.Models.Responses.Market
{
    public class OrderBookEntry
    {
        [JsonProperty("0")]
        public decimal Price { get; set; }

        [JsonProperty("1")]
        public decimal Quantity { get; set; }
    }

    public class MarketDepthData
    {
        [JsonProperty("asks")]
        public List<List<decimal>> Asks { get; set; }

        [JsonProperty("bids")]
        public List<List<decimal>> Bids { get; set; }
    }

    public class MarketDepthResponse
    {
        [JsonProperty("code")]
        public int Code { get; set; }

        [JsonProperty("data")]
        public MarketDepthData Data { get; set; }

        [JsonProperty("msg")]
        public string Msg { get; set; }
    }

}
