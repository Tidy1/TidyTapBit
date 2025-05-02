using TidyTapBit.ApiIntegration.Interfaces;
using TidyTapBit.Strategies.Interfaces;

namespace TidyTapBit.Strategies.Models
{
    public class MomentumScalpingStrategy
    {
        private readonly IMarketData _marketData;
        private readonly IBitunixApiClient _bitunixClient;

        public MomentumScalpingStrategy(IMarketData marketData, IBitunixApiClient bitunixClient)
        {
            _marketData = marketData;
            _bitunixClient = bitunixClient;
        }

        public async Task ExecuteTradeAsync(string symbol, decimal qty, CancellationToken cancellationToken)
        {
            try
            {
                // Get live market data
                decimal currentPrice = _marketData.GetLivePrice(symbol);
                decimal orderBookImbalance = _marketData.GetOrderBookSpread(symbol);
                decimal rsi = _marketData.GetMovingAverage(symbol, "14");
                decimal macd = _marketData.GetMovingAverage(symbol, "26") - _marketData.GetMovingAverage(symbol, "12");

                // Define profit and stop-loss targets
                decimal takeProfit = currentPrice * 1.0015m; // +0.15%
                decimal stopLoss = currentPrice * 0.9985m;   // -0.15%

                // Check Buy Entry Condition
                if (rsi > 55 && macd > 0 && orderBookImbalance > 0)
                {
                    Console.WriteLine($"[BUY] Placing order for {symbol} at {currentPrice}");
                    await _bitunixClient.PlaceFuturesOrderAsync(symbol, qty, "buy", "limit", 1);

                    // Monitor trade and exit at profit/loss levels
                    await MonitorTrade(symbol, currentPrice, takeProfit, stopLoss, "buy", cancellationToken);
                }

                // Check Sell Entry Condition
                if (rsi < 45 && macd < 0 && orderBookImbalance < 0)
                {
                    Console.WriteLine($"[SELL] Placing order for {symbol} at {currentPrice}");
                    await _bitunixClient.PlaceFuturesOrderAsync(symbol, qty, "sell", "limit", 1);

                    // Monitor trade and exit at profit/loss levels
                    await MonitorTrade(symbol, currentPrice, takeProfit, stopLoss, "sell", cancellationToken);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] {ex.Message}");
            }
        }

        private async Task MonitorTrade(string symbol, decimal entryPrice, decimal takeProfit, decimal stopLoss, string side, CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    decimal livePrice = _marketData.GetLivePrice(symbol);

                    if (side == "buy" && livePrice >= takeProfit)
                    {
                        Console.WriteLine($"[CLOSE] Take profit hit for {symbol} at {livePrice}");
                        await _bitunixClient.PlaceFuturesOrderAsync(symbol, 1, "sell", "market", 1);
                        break;
                    }

                    if (side == "sell" && livePrice <= stopLoss)
                    {
                        Console.WriteLine($"[CLOSE] Stop-loss hit for {symbol} at {livePrice}");
                        await _bitunixClient.PlaceFuturesOrderAsync(symbol, 1, "buy", "market", 1);
                        break;
                    }

                    await Task.Delay(500, cancellationToken); // Check price every 500ms
                }
            }
            catch (TaskCanceledException)
            {
                Console.WriteLine("[INFO] Monitoring cancelled.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] {ex.Message}");
            }
        }
    }
}
