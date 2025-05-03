namespace TidyTrader.Core.Models
{
    public class PerpetualTradeOrder : TradeOrder
    {
        public decimal Leverage { get; set; } // Leverage multiplier (e.g., 10x)
        public string MarginMode { get; set; } // "isolated" or "cross"
    }
}
