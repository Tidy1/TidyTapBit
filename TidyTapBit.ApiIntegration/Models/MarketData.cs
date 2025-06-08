using RestSharp;
using Newtonsoft.Json.Linq;
using TidyTrader.ApiIntegration.Interfaces;
using TidyTrader.ApiIntegration.Models.Responses.Market;
using TidyTrader.ApiIntegration.Models.Responses.Trade;
using Newtonsoft.Json;
using Skender.Stock.Indicators;

namespace TidyTrader.ApiIntegration.Models
{
    public class MarketData
    {
        private readonly IBitunixApiClient _bitunixClient;
        private LeverageConfig _leverageConfig;

        public MarketData(IBitunixApiClient bitunixClient, LeverageConfig leverageConfig)
        {
            _bitunixClient = bitunixClient;
            _leverageConfig = leverageConfig;
        }

        public void SetLeverageConfig(int minLeverage, int maxLeverage)
        {
            _leverageConfig.MinLeverage = minLeverage;
            _leverageConfig.MaxLeverage = maxLeverage;
        }

        public LeverageConfig GetLeverageConfig() => _leverageConfig;

        private static List<Quote> ToQuotes(IEnumerable<KlineItem> klines)
        {
            return klines.Select(k => new Quote
            {
                Date = DateTimeOffset.FromUnixTimeMilliseconds(k.Time).UtcDateTime,
                Open = k.Open,
                High = k.High,
                Low = k.Low,
                Close = k.Close,
                Volume = decimal.Parse(k.BaseVolume)
            }).ToList();
        }

        private int DetermineLeverage()
        {
            var random = new Random();
            return random.Next(_leverageConfig.MinLeverage, _leverageConfig.MaxLeverage + 1);
        }

        public async Task<string> GetExchangeInfoAsync() => "none";

        public async Task<MarketDepthResponse> GetOrderBookAsync(string symbol, string limit)
        {
            var response = await _bitunixClient.GetMarketDepthAsync(symbol, limit);
            return response.Data;
        }

        public async Task<KlineResponse> GetKlineDataAsync(string symbol, string interval, string limit)
        {
            var response = await _bitunixClient.GetKlineAsync(symbol, interval, null, null, int.Parse(limit), null);
            return response.Data;
        }

        public async Task<TickerResponse> GetTickerAsync(string symbol)
        {
            var response = await _bitunixClient.GetTickersAsync(symbol);
            return response.Data;
        }

        public async Task<FundingRateResponse> GetFundingRateAsync(string symbol)
        {
            var response = await _bitunixClient.GetFundingRateAsync(symbol);
            return response.Data;
        }

        public async Task<GetTradeHistoryResponse> GetRecentTradesAsync(string? symbol = null, long? startTime = null, long? endTime = null, int? limit = null)
        {
            var response = await _bitunixClient.GetHistoryTradesAsync(symbol, null, null, startTime, endTime, null, limit);
            return response.Data;
        }

        public async Task<GetPendingOrdersResponse> GetPendingTradesAsync(string? symbol = null, long? startTime = null, long? endTime = null, int? limit = null)
        {
            var response = await _bitunixClient.GetPendingOrdersAsync(symbol);
            return response.Data;
        }

        public async Task<decimal> GetLivePriceAsync(string symbol)
        {
            var content = await GetTickerAsync(symbol);

            decimal.TryParse(content.Data.First().Last, out decimal result);

            return result;
        }

        public async Task<decimal> GetMovingAverageAsync(string symbol, string interval, int period)
        {
            var klineResponse = await GetKlineDataAsync(symbol, interval, period.ToString());
            var data = klineResponse.Data;

            if (data == null || data.Count < period) return 0m;

            return data
                .Take(period)
                .Select(k => k.Close)
                .Average();
        }

        public async Task<decimal> GetOrderBookSpreadAsync(string symbol)
        {
            var book = await GetOrderBookAsync(symbol, "5");

            var bestBid = book.Data.Bids[0][0];
            var bestAsk = book.Data.Asks[0][0];

            return bestAsk - bestBid;
        }

        public async Task<(decimal bidVolume, decimal askVolume)> GetOrderBookVolumesAsync(string symbol)
        {
            var book = await GetOrderBookAsync(symbol, "5");

            var bidVolume = book.Data.Bids.Take(5).Sum(b => b[1]);
            var askVolume = book.Data.Asks.Take(5).Sum(a => a[1]);

            return (bidVolume, askVolume);
        }

        public async Task<MarketMetrics> GetMarketMetricsAsync(string symbol, string interval = "1m", int maPeriod = 20)
        {
            var ticker = await GetTickerAsync(symbol);
            var depth = await GetOrderBookAsync(symbol, "5");
            var funding = await GetFundingRateAsync(symbol);
            var ma = await GetMovingAverageAsync(symbol, interval, maPeriod);

            var lastPrice = decimal.Parse(ticker.Data.FirstOrDefault(t => t.Symbol == symbol)?.Last ?? "0");
            var markPrice = decimal.Parse(ticker.Data.FirstOrDefault(t => t.Symbol == symbol)?.MarkPrice ?? "0");

            var bestBid = depth.Data.Bids[0][0];
            var bestAsk = depth.Data.Asks[0][0];

            var bidVolume = depth.Data.Bids.Take(5).Sum(b => b[1]);
            var askVolume = depth.Data.Asks.Take(5).Sum(a => a[1]);

            return new MarketMetrics
            {
                Symbol = symbol,
                LastPrice = lastPrice,
                MarkPrice = markPrice,
                MovingAverage = ma,
                BestBid = bestBid,
                BestAsk = bestAsk,
                BidVolume = bidVolume,
                AskVolume = askVolume,
                FundingRate = funding.Data.FundingRate
            };
        }

        public async Task<decimal> GetVwapAsync(string symbol, string interval = "1m")
        {
            var kline = await GetKlineDataAsync(symbol, interval, "100");
            var quotes = ToQuotes(kline.Data);
            return Convert.ToDecimal(quotes.GetVwap().LastOrDefault()?.Vwap ?? 0);
        }

        public async Task<decimal> GetPriceMomentumAsync(string symbol, string interval = "1m", int period = 10)
        {
            var kline = await GetKlineDataAsync(symbol, interval, (period + 5).ToString());
            var quotes = ToQuotes(kline.Data);
            return Convert.ToDecimal(quotes.GetRoc(period).LastOrDefault()?.Momentum ?? 0);
        }

        public async Task<(decimal UpperBand, decimal LowerBand)> GetBollingerBandsAsync(string symbol, string interval = "1m", int period = 20, decimal stdDevMultiplier = 2)
        {
            var kline = await GetKlineDataAsync(symbol, interval, period.ToString());
            if (kline?.Data == null || kline.Data.Count < period) return (0m, 0m);

            var closes = kline.Data.Select(x => x.Close).Take(period).ToList();
            var average = closes.Average();
            var stdDev = (decimal)Math.Sqrt(closes.Select(p => (double)(p - average) * (double)(p - average)).Average());

            var upper = average + stdDevMultiplier * stdDev;
            var lower = average - stdDevMultiplier * stdDev;
            return (upper, lower);
        }

        public async Task<decimal> GetSmaAsync(string symbol, string interval = "1m", int period = 20)
        {
            var kline = await GetKlineDataAsync(symbol, interval, (period + 20).ToString());
            var quotes = ToQuotes(kline.Data);
            return Convert.ToDecimal(quotes.GetSma(period).LastOrDefault()?.Sma ?? 0);
        }

        public async Task<decimal> GetRsiAsync(string symbol, string interval = "1m", int period = 14)
        {
            var kline = await GetKlineDataAsync(symbol, interval, (period + 20).ToString());
            var quotes = ToQuotes(kline.Data);
            return Convert.ToDecimal(quotes.GetRsi(period).LastOrDefault()?.Rsi ?? 0);
        }

        public async Task<string> DetectCandlePatternAsync(string symbol, string interval = "1m")
        {
            var kline = await GetKlineDataAsync(symbol, interval, "3");
            if (kline?.Data == null || kline.Data.Count < 3) return "Insufficient data";

            var candle = kline.Data[0];
            var body = Math.Abs(candle.Close - candle.Open);
            var range = candle.High - candle.Low;

            if (body < range * 0.2m) return "Doji";
            if (candle.Close > candle.Open && kline.Data[1].Close < kline.Data[1].Open && candle.Open < kline.Data[1].Close && candle.Close > kline.Data[1].Open) return "Bullish Engulfing";
            if (candle.Close < candle.Open && kline.Data[1].Close > kline.Data[1].Open && candle.Open > kline.Data[1].Close && candle.Close < kline.Data[1].Open) return "Bearish Engulfing";

            return "No recognizable pattern";
        }
    }
}
