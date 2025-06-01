using System.Collections.Generic;
using System.Linq;

using Skender.Stock.Indicators;

namespace TidyTapBit.Core.Indicators
{
    public class TechnicalIndicatorService
    {
        public IndicatorResults GetIndicators(IEnumerable<Quote> quotes)
        {
            // Ensure quotes are sorted by Date ascending
            var sorted = quotes.OrderBy(q => q.Date);
            var basePeriods = 100;
            return new IndicatorResults
            {
                // Average Directional Index
                Adx = sorted.GetAdx(basePeriods).RemoveWarmupPeriods().ToList(),

                Atr = sorted.GetAtr(basePeriods).RemoveWarmupPeriods().ToList(),
                BollingerBands = sorted.GetBollingerBands(basePeriods, 2).RemoveWarmupPeriods().ToList(),
                Cci = sorted.GetCci(basePeriods).RemoveWarmupPeriods().ToList(),
                Cmf = sorted.GetCmf(basePeriods).RemoveWarmupPeriods().ToList(),
                Dema = sorted.GetDema(basePeriods).RemoveWarmupPeriods().ToList(),
                Ema = sorted.GetEma(basePeriods).RemoveWarmupPeriods().ToList(),
                Hma = sorted.GetHma(basePeriods).RemoveWarmupPeriods().ToList(),
                Macd = sorted.GetMacd(50, basePeriods, 9).ToList(),
                Mfi = sorted.GetMfi(basePeriods).RemoveWarmupPeriods().ToList(),                
                Obv = sorted.GetObv().ToList(),
                Psar = sorted.GetParabolicSar(0.02, 0.2).ToList(),
                Roc = sorted.GetRoc(basePeriods).RemoveWarmupPeriods().ToList(),
                Rsi = sorted.GetRsi(basePeriods).RemoveWarmupPeriods().ToList(),
                Sma = sorted.GetSma(basePeriods).RemoveWarmupPeriods().ToList(),
                Stoch = sorted.GetStoch(basePeriods, 3, 3).RemoveWarmupPeriods().ToList(),
                StochRsi = sorted.GetStochRsi(basePeriods, basePeriods, 3, 3).RemoveWarmupPeriods().ToList(),
                Tema = sorted.GetTema(basePeriods).RemoveWarmupPeriods().ToList(),
                Wma = sorted.GetWma(basePeriods).RemoveWarmupPeriods().ToList(),
            };
        }
    }

    public class IndicatorResults
    {
        public List<SmaResult> Sma { get; set; }
        public List<EmaResult> Ema { get; set; }
        public List<WmaResult> Wma { get; set; }
        public List<HmaResult> Hma { get; set; }
        public List<DemaResult> Dema { get; set; }
        public List<TemaResult> Tema { get; set; }
        public List<RsiResult> Rsi { get; set; }
        public List<MacdResult> Macd { get; set; }
        public List<BollingerBandsResult> BollingerBands { get; set; }
        public List<ParabolicSarResult> Psar { get; set; }
        public List<AtrResult> Atr { get; set; }
        public List<ObvResult> Obv { get; set; }
        public List<AdxResult> Adx { get; set; }
        public List<CciResult> Cci { get; set; }
        public List<RocResult> Roc { get; set; }
        public List<StochResult> Stoch { get; set; }
        public List<StochRsiResult> StochRsi { get; set; }
        public List<CmfResult> Cmf { get; set; }
        public List<MfiResult> Mfi { get; set; }
    }
}
