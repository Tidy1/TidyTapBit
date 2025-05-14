using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using TidyTrader.ApiIntegration.Interfaces;
using TidyTrader.ApiIntegration.Models.Responses.Market;

namespace TidyTrader.Core.Models
{
    public class MarketService
    {
        private readonly IMarketData _marketData;
        public MarketService(IMarketData marketData)
        {
            _marketData = marketData;
        }

        public async Task<decimal> GetPriceAsync(string symbol)
        {
            // this now returns a typed model
            TickerResponse resp = await _marketData.GetTickerAsync(symbol);
            // for example, take the first ticker’s Last price
            if (resp?.Data?.Any() == true &&
                decimal.TryParse(resp.Data[0].Last, out var last))
                return last;

            throw new InvalidOperationException("No ticker data");
        }
    }
}
