using TidyTapBit.Core.Interfaces;
using TidyTapBit.Core.Models;

namespace TidyTapBit.StrategyExecutor.Models
{
    public class TradingBot
    {
        private readonly ITradingApiClient _apiClient;
        private readonly TradingStrategyParameters _parameters;
        private readonly List<TradeHistory> _tradeHistory;
        private decimal _currentInvestment;

        public TradingBot(ITradingApiClient apiClient, TradingStrategyParameters parameters)
        {
            _apiClient = apiClient;
            _parameters = parameters;
            _currentInvestment = _parameters.InitialInvestment;
            _tradeHistory = new List<TradeHistory>();
        }

        public async Task ExecuteStrategyAsync(string symbol)
        {
            Console.WriteLine($"Starting trading strategy for {symbol}...");

            var initialPrice = await GetMarketPriceAsync(symbol);
            Console.WriteLine($"Initial price for {symbol}: {initialPrice}");

            while (true)
            {
                var currentPrice = await GetMarketPriceAsync(symbol);
                var priceChangePercentage = ((currentPrice - initialPrice) / initialPrice) * 100;

                Console.WriteLine($"Current price: {currentPrice}, Change: {priceChangePercentage}%");

                if (Math.Abs(priceChangePercentage) >= _parameters.TargetPercentage)
                {
                    var quantity = CalculateQuantity(currentPrice);
                    var fees = CalculateFees(quantity, currentPrice);
                    var marginCost = CalculateMarginCost(quantity, currentPrice);

                    Console.WriteLine($"Placing order: {quantity} {symbol} at {currentPrice}");
                    Console.WriteLine($"Fees: {fees}, Margin Cost: {marginCost}");

                    var order = new PerpetualTradeOrder
                    {
                        Symbol = symbol,
                        Side = "buy",
                        Quantity = quantity,
                        Price = currentPrice,
                        OrderType = "limit",
                        Leverage = _parameters.Leverage,
                        MarginMode = "isolated"
                    };

                    var response = await _apiClient.PlacePerpetualOrderAsync(order);
                    Console.WriteLine($"Order Response: {response}");

                    var profit = CalculateProfit(currentPrice, quantity, fees, marginCost);
                    var reinvestment = profit * (_parameters.ReinvestmentPercentage / 100);

                    _currentInvestment += reinvestment;
                    Console.WriteLine($"Profit: {profit}, Reinvestment: {reinvestment}, New Investment: {_currentInvestment}");

                    _tradeHistory.Add(new TradeHistory
                    {
                        Symbol = symbol,
                        Side = "buy",
                        Quantity = quantity,
                        EntryPrice = currentPrice,
                        ExitPrice = 0, // To be updated when the trade is closed
                        Profit = profit,
                        Timestamp = DateTime.UtcNow
                    });

                    initialPrice = currentPrice;
                }

                await Task.Delay(5000);
            }
        }

        private async Task<decimal> GetMarketPriceAsync(string symbol)
        {
            return await Task.FromResult(new Random().Next(1000, 2000)); // Replace with real API call
        }

        private decimal CalculateQuantity(decimal currentPrice)
        {
            return _currentInvestment / currentPrice;
        }

        private decimal CalculateFees(decimal quantity, decimal currentPrice)
        {
            var tradeAmount = quantity * currentPrice;
            return tradeAmount * (_parameters.MakerFee / 100);
        }

        private decimal CalculateMarginCost(decimal quantity, decimal currentPrice)
        {
            var tradeAmount = quantity * currentPrice;
            return tradeAmount / _parameters.Leverage;
        }

        private decimal CalculateProfit(decimal currentPrice, decimal quantity, decimal fees, decimal marginCost)
        {
            var tradeAmount = quantity * currentPrice;
            return tradeAmount - fees - marginCost;
        }

        public List<TradeHistory> GetTradeHistory()
        {
            return _tradeHistory;
        }
    }
}
