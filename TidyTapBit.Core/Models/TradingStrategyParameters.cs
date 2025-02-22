namespace TidyTapBit.Core.Models
{
    public class TradingStrategyParameters
    {
        public decimal InitialInvestment { get; set; } // Starting amount, e.g., $10
        public decimal TargetPercentage { get; set; } // Price change to trigger buy, e.g., 5%
        public decimal ReinvestmentPercentage { get; set; } // % of profit to reinvest, e.g., 50%
        public decimal MakerFee { get; set; } // Maker fee percentage, e.g., 0.1%
        public decimal TakerFee { get; set; } // Taker fee percentage, e.g., 0.2%
        public decimal Leverage { get; set; } // Leverage for margin trading, e.g., 10x
    }
}
