using TidyTapBit.Core.Models;

using TidyTrader.Core.Interfaces;

namespace TidyTrader.Strategies.Models
{
    public class ArbitrageStrategy
    {
        // Executes high-frequency buy/sell orders based on price threshold
        public async Task<string> ExecuteHighFrequencyTradesAsync(ITradingApiClient apiClient, string symbol, decimal buyThreshold, decimal sellThreshold, decimal quantity, decimal price)
        {
            if (price <= buyThreshold)
            {
                await apiClient.PlacePerpetualOrderAsync(new PerpetualTradeOrder
                {
                    Symbol = symbol,
                    Side = "buy",
                    Quantity = quantity,
                    Price = price,
                    OrderType = "limit",
                    Leverage = 50,
                    MarginMode = "isolated"
                });
                return "Buy Order Placed";
            }
            if (price >= sellThreshold)
            {
                await apiClient.PlacePerpetualOrderAsync(new PerpetualTradeOrder
                {
                    Symbol = symbol,
                    Side = "sell",
                    Quantity = quantity,
                    Price = price,
                    OrderType = "limit",
                    Leverage = 50,
                    MarginMode = "isolated"
                });
                return "Sell Order Placed";
            }
            return "No Trade";
        }

        // Order Book Imbalance: Measures bid-ask volume imbalance
        public string GenerateOrderBookImbalanceSignal(decimal bidVolume, decimal askVolume)
        {
            decimal imbalance = bidVolume - askVolume;
            if (imbalance > 0) return "Buy";
            if (imbalance < 0) return "Sell";
            return "Hold";
        }

        // Triangular Arbitrage: Identifies inefficiencies in currency pairs
        public string GenerateTriangularArbitrageSignal(decimal basePrice, decimal crossPrice, decimal targetPrice)
        {
            if (basePrice * crossPrice > targetPrice) return "Buy";
            if (basePrice * crossPrice < targetPrice) return "Sell";
            return "Hold";
        }

        // Latency Arbitrage: Trades based on price differences across exchanges
        public string GenerateLatencyArbitrageSignal(decimal fastExchangePrice, decimal slowExchangePrice)
        {
            if (fastExchangePrice < slowExchangePrice) return "Buy";
            if (fastExchangePrice > slowExchangePrice) return "Sell";
            return "Hold";
        }
    }
}
