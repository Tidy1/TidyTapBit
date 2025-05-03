namespace TidyTrader.Core.Models
{
    public class SpotTradeOrder : TradeOrder
    {
        public decimal MarginAmount { get; set; } // Margin required for the trade
    }
}
