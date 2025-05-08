using Newtonsoft.Json;

namespace TidyTrader.ApiIntegration.Models.Responses.Market
{
    public class TickerItem
    {
        [JsonProperty("symbol")]
        public string Symbol { get; set; }

        [JsonProperty("markPrice")]
        public string MarkPrice { get; set; }

        [JsonProperty("lastPrice")]
        public string LastPrice { get; set; }

        [JsonProperty("open")]
        public string Open { get; set; }

        [JsonProperty("last")]
        public string Last { get; set; }

        [JsonProperty("quoteVol")]
        public string QuoteVolume { get; set; }

        [JsonProperty("baseVol")]
        public string BaseVolume { get; set; }

        [JsonProperty("high")]
        public string High { get; set; }

        [JsonProperty("low")]
        public string Low { get; set; }
    }

    public class TickerResponse
    {
        [JsonProperty("code")]
        public int Code { get; set; }

        [JsonProperty("data")]
        public List<TickerItem> Data { get; set; }

        [JsonProperty("msg")]
        public string Msg { get; set; }
    }

}
