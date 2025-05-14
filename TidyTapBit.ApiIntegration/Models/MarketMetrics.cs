namespace TidyTrader.ApiIntegration.Models
{
    public class MarketMetrics
    {
        public string Symbol { get; set; }

        public decimal LastPrice { get; set; }
        public decimal MarkPrice { get; set; }

        public decimal MovingAverage { get; set; }

        public decimal BestBid { get; set; }
        public decimal BestAsk { get; set; }
        public decimal Spread => BestAsk - BestBid;

        public decimal BidVolume { get; set; }
        public decimal AskVolume { get; set; }

        public decimal FundingRate { get; set; }

        public DateTime RetrievedAt { get; set; } = DateTime.UtcNow;
    }
}