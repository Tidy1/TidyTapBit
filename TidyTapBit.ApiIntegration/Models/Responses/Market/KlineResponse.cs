using Newtonsoft.Json;

namespace TidyTrader.ApiIntegration.Models.Responses.Market
{
    public class KlineItem
    {
        [JsonProperty("open")]
        public decimal Open { get; set; }

        [JsonProperty("high")]
        public decimal High { get; set; }

        [JsonProperty("low")]
        public decimal Low { get; set; }

        [JsonProperty("close")]
        public decimal Close { get; set; }

        [JsonProperty("quoteVol")]
        public string QuoteVolume { get; set; }

        [JsonProperty("baseVol")]
        public string BaseVolume { get; set; }

        [JsonProperty("time")]
        public long Time { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }
    }

    public class KlineResponse
    {
        [JsonProperty("code")]
        public int Code { get; set; }

        [JsonProperty("data")]
        public List<KlineItem> Data { get; set; }

        [JsonProperty("msg")]
        public string Msg { get; set; }
    }

}
