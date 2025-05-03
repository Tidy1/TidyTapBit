namespace TidyTrader.Strategies.Models
{
    public class MeanReversionStrategy
    {
        // Consolidate similar methods into one generic method
        private string GenerateSignal(decimal value, decimal upperThreshold, decimal lowerThreshold, string upperSignal, string lowerSignal)
        {
            if (value > upperThreshold) return upperSignal;
            if (value < lowerThreshold) return lowerSignal;
            return "Hold";
        }

        public string GenerateZScoreSignal(decimal currentPrice, decimal movingAverage, decimal stdDev)
        {
            decimal zScore = (currentPrice - movingAverage) / stdDev;
            return GenerateSignal(zScore, 2, -2, "Sell", "Buy");
        }

        public string GenerateRSISignal(decimal rsiValue)
        {
            return GenerateSignal(rsiValue, 70, 30, "Sell", "Buy");
        }

        public string GenerateBollingerBandsSignal(decimal price, decimal upperBand, decimal lowerBand)
        {
            return GenerateSignal(price, upperBand, lowerBand, "Sell", "Buy");
        }

        public string GenerateMeanCrossoverSignal(decimal price, decimal mean)
        {
            return GenerateSignal(price, mean, mean, "Sell", "Buy");
        }

        public string GenerateVWAPSignal(decimal price, decimal vwap)
        {
            return GenerateSignal(price, vwap, vwap, "Sell", "Buy");
        }

        public string GenerateKeltnerChannelSignal(decimal price, decimal upperChannel, decimal lowerChannel)
        {
            return GenerateSignal(price, upperChannel, lowerChannel, "Sell", "Buy");
        }

        public string GenerateDonchianChannelSignal(decimal price, decimal highestPrice, decimal lowestPrice)
        {
            return GenerateSignal(price, highestPrice, lowestPrice, "Sell", "Buy");
        }

        public string GenerateParabolicSARSignal(decimal currentPrice, decimal sar)
        {
            return GenerateSignal(currentPrice, sar, sar, "Sell", "Buy");
        }

        public string GenerateCCISignal(decimal cci, decimal threshold)
        {
            return GenerateSignal(cci, threshold, -threshold, "Sell", "Buy");
        }

        public string GenerateATRBandSignal(decimal price, decimal upperATRBand, decimal lowerATRBand)
        {
            return GenerateSignal(price, upperATRBand, lowerATRBand, "Sell", "Buy");
        }
    }
}
