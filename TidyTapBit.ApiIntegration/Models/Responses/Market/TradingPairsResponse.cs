using Newtonsoft.Json;

namespace TidyTrader.ApiIntegration.Models.Responses.Market
{
    public class TradingPairItem
    {
        [JsonProperty("symbol")]
        public string Symbol { get; set; }

        [JsonProperty("base")]
        public string Base { get; set; }

        [JsonProperty("quote")]
        public string Quote { get; set; }

        [JsonProperty("minTradeVolume")]
        public string MinTradeVolume { get; set; }

        [JsonProperty("minBuyPriceOffset")]
        public string MinBuyPriceOffset { get; set; }

        [JsonProperty("maxSellPriceOffset")]
        public string MaxSellPriceOffset { get; set; }

        [JsonProperty("maxLimitOrderVolume")]
        public string MaxLimitOrderVolume { get; set; }

        [JsonProperty("maxMarketOrderVolume")]
        public string MaxMarketOrderVolume { get; set; }

        [JsonProperty("basePrecision")]
        public int BasePrecision { get; set; }

        [JsonProperty("quotePrecision")]
        public int QuotePrecision { get; set; }

        [JsonProperty("minLeverage")]
        public int MinLeverage { get; set; }

        [JsonProperty("maxLeverage")]
        public int MaxLeverage { get; set; }

        [JsonProperty("defaultLeverage")]
        public int DefaultLeverage { get; set; }

        [JsonProperty("defaultMarginMode")]
        public int DefaultMarginMode { get; set; }

        [JsonProperty("priceProtectScope")]
        public string PriceProtectScope { get; set; }

        [JsonProperty("symbolStatus")]
        public string SymbolStatus { get; set; }
    }

    public class TradingPairsResponse
    {
        [JsonProperty("code")]
        public int Code { get; set; }

        [JsonProperty("data")]
        public List<TradingPairItem> Data { get; set; }

        [JsonProperty("msg")]
        public string Msg { get; set; }
    }

}
