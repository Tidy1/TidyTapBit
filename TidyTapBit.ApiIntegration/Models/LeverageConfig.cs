namespace TidyTrader.ApiIntegration.Models
{
    public class LeverageConfig
    {
        public int MinLeverage { get; set; }
        public int MaxLeverage { get; set; }

        public LeverageConfig(int minLeverage, int maxLeverage)
        {
            MinLeverage = minLeverage;
            MaxLeverage = maxLeverage;
        }
    }
}