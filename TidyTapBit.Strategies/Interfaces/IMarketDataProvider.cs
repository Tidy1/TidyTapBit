using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TidyTapBit.Strategies.Interfaces
{
    public interface IMarketDataProvider
    {
        decimal GetLivePrice(string symbol);
        decimal GetMovingAverage(string symbol, int period);
        decimal GetOrderBookSpread(string symbol);
        (decimal bidVolume, decimal askVolume) GetOrderBookVolumes(string symbol);
    }
}
