namespace TidyTapBit.Strategies.Models
{
    public class MeanReversionStrategy
    {
        // Z-Score: Measures deviation from mean price
        public string GenerateZScoreSignal(decimal currentPrice, decimal movingAverage, decimal stdDev)
        {
            decimal zScore = (currentPrice - movingAverage) / stdDev;
            if (zScore < -2) return "Buy";
            if (zScore > 2) return "Sell";
            return "Hold";
        }

        // RSI: Identifies overbought and oversold conditions
        public string GenerateRSISignal(decimal rsiValue)
        {
            if (rsiValue < 30) return "Buy";
            if (rsiValue > 70) return "Sell";
            return "Hold";
        }

        // Bollinger Bands: Detects price deviations from volatility bands
        public string GenerateBollingerBandsSignal(decimal price, decimal upperBand, decimal lowerBand)
        {
            if (price > upperBand) return "Sell";
            if (price < lowerBand) return "Buy";
            return "Hold";
        }

        // Mean Crossover: Checks if price crosses mean
        public string GenerateMeanCrossoverSignal(decimal price, decimal mean)
        {
            if (price > mean) return "Sell";
            if (price < mean) return "Buy";
            return "Hold";
        }

        // VWAP: Compares price with volume-weighted average price
        public string GenerateVWAPSignal(decimal price, decimal vwap)
        {
            if (price > vwap) return "Sell";
            if (price < vwap) return "Buy";
            return "Hold";
        }

        // Keltner Channel: Identifies overbought/oversold conditions
        public string GenerateKeltnerChannelSignal(decimal price, decimal upperChannel, decimal lowerChannel)
        {
            if (price > upperChannel) return "Sell";
            if (price < lowerChannel) return "Buy";
            return "Hold";
        }

        // Donchian Channel: Measures highest/lowest price in a period
        public string GenerateDonchianChannelSignal(decimal price, decimal highestPrice, decimal lowestPrice)
        {
            if (price > highestPrice) return "Sell";
            if (price < lowestPrice) return "Buy";
            return "Hold";
        }

        // Parabolic SAR: Identifies trend reversals
        public string GenerateParabolicSARSignal(decimal currentPrice, decimal sar)
        {
            if (currentPrice > sar) return "Sell";
            if (currentPrice < sar) return "Buy";
            return "Hold";
        }

        // CCI: Commodity Channel Index detects price deviations
        public string GenerateCCISignal(decimal cci, decimal threshold)
        {
            if (cci > threshold) return "Sell";
            if (cci < -threshold) return "Buy";
            return "Hold";
        }

        // ATR Band Mean Reversion: Uses ATR to detect volatility extremes
        public string GenerateATRBandSignal(decimal price, decimal upperATRBand, decimal lowerATRBand)
        {
            if (price > upperATRBand) return "Sell";
            if (price < lowerATRBand) return "Buy";
            return "Hold";
        }
    }
}
