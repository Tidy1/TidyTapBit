namespace TidyTapBit.Strategies.Models
{
    public class MomentumStrategy
    {
        // SMA Crossover: Short SMA crosses above/below long SMA
        public string GenerateSMACrossoverSignal(decimal shortSMA, decimal longSMA)
        {
            if (shortSMA > longSMA) return "Buy";
            if (shortSMA < longSMA) return "Sell";
            return "Hold";
        }

        // MACD: Moving Average Convergence Divergence
        public string GenerateMACDSignal(decimal macd, decimal signalLine)
        {
            if (macd > signalLine) return "Buy";
            if (macd < signalLine) return "Sell";
            return "Hold";
        }

        // ADX: Measures trend strength
        public string GenerateADXSignal(decimal adx, decimal threshold)
        {
            if (adx > threshold) return "Buy";
            return "Hold";
        }

        // Momentum Oscillator: Identifies momentum direction
        public string GenerateMomentumOscillatorSignal(decimal momentum)
        {
            if (momentum > 0) return "Buy";
            if (momentum < 0) return "Sell";
            return "Hold";
        }

        // RSI Momentum: Checks RSI strength
        public string GenerateRSIMomentumSignal(decimal rsi, decimal threshold)
        {
            if (rsi > threshold) return "Buy";
            if (rsi < (100 - threshold)) return "Sell";
            return "Hold";
        }

        // Bollinger Band Breakout: Identifies breakouts above/below bands
        public string GenerateBollingerBandBreakoutSignal(decimal price, decimal upperBand, decimal lowerBand)
        {
            if (price > upperBand) return "Buy";
            if (price < lowerBand) return "Sell";
            return "Hold";
        }

        // Rate of Change: Measures price momentum
        public string GenerateRateOfChangeSignal(decimal roc, decimal threshold)
        {
            if (roc > threshold) return "Buy";
            if (roc < -threshold) return "Sell";
            return "Hold";
        }

        // Stochastic Oscillator: Measures momentum vs. price range
        public string GenerateStochasticOscillatorSignal(decimal stochasticK, decimal stochasticD)
        {
            if (stochasticK > stochasticD) return "Buy";
            if (stochasticK < stochasticD) return "Sell";
            return "Hold";
        }

        // Williams %R: Identifies overbought and oversold conditions
        public string GenerateWilliamsRSignal(decimal williamsR)
        {
            if (williamsR > -20) return "Sell";
            if (williamsR < -80) return "Buy";
            return "Hold";
        }

        // CCI: Commodity Channel Index measures trend strength
        public string GenerateCCISignal(decimal cci, decimal threshold)
        {
            if (cci > threshold) return "Buy";
            if (cci < -threshold) return "Sell";
            return "Hold";
        }

        // Heikin-Ashi Trend: Identifies bullish/bearish trends
        public string GenerateHeikinAshiTrendSignal(decimal currentClose, decimal priorClose)
        {
            if (currentClose > priorClose) return "Buy";
            if (currentClose < priorClose) return "Sell";
            return "Hold";
        }

        // Keltner Channel: Identifies volatility-based breakouts
        public string GenerateKeltnerChannelSignal(decimal price, decimal upperChannel, decimal lowerChannel)
        {
            if (price > upperChannel) return "Buy";
            if (price < lowerChannel) return "Sell";
            return "Hold";
        }

        // Chande Momentum Oscillator: Measures trend direction
        public string GenerateChandeMomentumSignal(decimal cmo, decimal threshold)
        {
            if (cmo > threshold) return "Buy";
            if (cmo < -threshold) return "Sell";
            return "Hold";
        }

        // Detrended Price Oscillator: Removes trend component
        public string GenerateDPOOscillatorSignal(decimal dpo, decimal threshold)
        {
            if (dpo > threshold) return "Buy";
            if (dpo < -threshold) return "Sell";
            return "Hold";
        }

        // TRIX Indicator: Measures rate of change of a triple smoothed EMA
        public string GenerateTRIXSignal(decimal trix, decimal signalLine)
        {
            if (trix > signalLine) return "Buy";
            if (trix < signalLine) return "Sell";
            return "Hold";
        }
    }
}
