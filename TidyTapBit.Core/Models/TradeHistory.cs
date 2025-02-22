namespace TidyTapBit.Core.Models
{
    public class TradeHistory
    {
        public string Symbol { get; set; }
        public string Side { get; set; }
        public decimal Quantity { get; set; }
        public decimal EntryPrice { get; set; }
        public decimal ExitPrice { get; set; }
        public decimal Profit { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
