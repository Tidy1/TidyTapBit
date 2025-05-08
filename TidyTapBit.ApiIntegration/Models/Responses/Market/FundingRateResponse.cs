using Newtonsoft.Json;

namespace TidyTrader.ApiIntegration.Models.Responses.Market
{
    public class FundingRateItem
    {
        [JsonProperty("symbol")]
        public string Symbol { get; set; }

        [JsonProperty("markPrice")]
        public decimal MarkPrice { get; set; }

        [JsonProperty("lastPrice")]
        public decimal LastPrice { get; set; }

        [JsonProperty("fundingRate")]
        public decimal FundingRate { get; set; }
    }

    public class FundingRateResponse
    {
        [JsonProperty("code")]
        public int Code { get; set; }

        [JsonProperty("data")]
        public List<FundingRateItem> Data { get; set; }

        [JsonProperty("msg")]
        public string Msg { get; set; }
    }

}
