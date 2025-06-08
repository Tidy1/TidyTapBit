using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TidyTrader.ApiIntegration.LadderStrategy
{
    /// <summary>
    /// Which side an order is on.
    /// </summary>
    public enum OrderSide
    {
        Long,
        Short
    }

    /// <summary>
    /// A single rung in the ladder: price, side, and (once placed) the order ID.
    /// </summary>
    public record Rung(decimal Price, OrderSide Side)
    {
        public string? OrderId { get; init; }
    }

    /// <summary>
    /// Configuration for the ladder: how many rungs, spacing multiplier, TP/SL %, etc.
    /// </summary>
    public class LadderConfig
    {
        /// <summary>How many rungs per side when (re)seeding the ladder.</summary>
        public int BaseRungsPerSide { get; init; } = 5;

        /// <summary>Multiplier applied to ATR (or any spacing source) to get rung spacing.</summary>
        public decimal SpacingMultiplier { get; init; } = 1.0m;

        /// <summary>Take-profit as a fraction of price (e.g. 0.005 → 0.5%).</summary>
        public decimal TakeProfitPct { get; init; } = 0.005m;

        /// <summary>Stop-loss as a fraction of price (e.g. 0.005 → 0.5%).</summary>
        public decimal StopLossPct { get; init; } = 0.005m;

        /// <summary>If you want to nudge TP/SL by the funding rate or fees.</summary>
        public decimal FundingRateAdjustment { get; init; } = 0m;

        /// <summary>How many TP hits on one side trigger a recenter.</summary>
        public int RungsToTpRecenter { get; init; } = 2;
    }

    /// <summary>
    /// Low-level interface for placing/canceling orders and detecting TP fills.
    /// </summary>
    public interface IOrderService
    {
        Task<string?> PlaceLimitOrderAsync(
            string symbol,
            decimal price,
            OrderSide side,
            decimal takeProfitPrice,
            decimal stopLossPrice);

        Task CancelOrdersAsync(IEnumerable<string> orderIds);
        bool WasTakeProfitFill(string orderId);
    }

    /// <summary>
    /// Strategy interface for any ladder-style algorithm.
    /// </summary>
    public interface ILadderStrategy
    {
        Task InitializeAsync(decimal livePrice);
        Task OnOrderFilledAsync(string orderId, OrderSide side, decimal fillPrice);
        Task OnPriceTickAsync(decimal livePrice);
        event Action<IEnumerable<Rung>>? OnRungsUpdated;
    }

    /// <summary>
    /// A “smart” ladder that, upon enough TP hits on one side, recenters the entire ladder.
    /// </summary>
    public class SmartRecenterLadder : ILadderStrategy
    {
        private readonly string _symbol;
        private readonly LadderConfig _cfg;
        private readonly IOrderService _orders;
        private readonly Func<Task<decimal>> _getCurrentSpacing;
        private readonly Func<decimal, decimal> _quantizePrice;

        private decimal _centerPrice;
        private bool _hasSeeded;
        private readonly Dictionary<OrderSide, List<Rung>> _rungs = new()
        {
            [OrderSide.Long] = new List<Rung>(),
            [OrderSide.Short] = new List<Rung>()
        };
        private readonly Dictionary<OrderSide, int> _tpHits = new() { [OrderSide.Long] = 0, [OrderSide.Short] = 0 };

        public event Action<IEnumerable<Rung>>? OnRungsUpdated;

        public SmartRecenterLadder(
            string symbol,
            LadderConfig config,
            IOrderService orderService,
            Func<Task<decimal>> getCurrentSpacing,
            Func<decimal, decimal> quantizePrice)
        {
            _symbol = symbol;
            _cfg = config;
            _orders = orderService;
            _getCurrentSpacing = getCurrentSpacing;
            _quantizePrice = quantizePrice;
        }

        public async Task InitializeAsync(decimal livePrice)
        {
            if (_hasSeeded) return;
            _hasSeeded = true;
            _centerPrice = livePrice;
            Console.WriteLine($"[{_symbol}] INITIALIZING ladder @ {livePrice:F6}");
            await RecenterLadderAsync(livePrice);
        }

        public async Task OnOrderFilledAsync(string orderId, OrderSide side, decimal fillPrice)
        {
            if (_orders.WasTakeProfitFill(orderId))
                _tpHits[side]++;

            if (_tpHits[side] >= _cfg.RungsToTpRecenter)
                await RecenterLadderAsync(fillPrice);
        }

        public async Task OnPriceTickAsync(decimal livePrice)
        {
            if (!_hasSeeded) return;

            var spacing = await _getCurrentSpacing();
            var longMin = _rungs[OrderSide.Long].Min(r => r.Price);
            var shortMax = _rungs[OrderSide.Short].Max(r => r.Price);

            if (livePrice < longMin - spacing || livePrice > shortMax + spacing)
                await RecenterLadderAsync(livePrice);
        }

        private async Task RecenterLadderAsync(decimal newCenter)
        {
            Console.WriteLine($"\n[{_symbol}] Shifting ladder to new center = {newCenter:F6}\n");

            _centerPrice = newCenter;
            _tpHits[OrderSide.Long] = 0;
            _tpHits[OrderSide.Short] = 0;

            var toCancel = _rungs.SelectMany(kv => kv.Value)
                                 .Select(r => r.OrderId)
                                 .Where(id => id != null)!
                                 .Distinct()
                                 .ToList();
            if (toCancel.Any())
                await _orders.CancelOrdersAsync(toCancel);

            var spacing = await _getCurrentSpacing();
            var rawLongs = BuildRungs(newCenter, OrderSide.Long, _cfg.BaseRungsPerSide, spacing);
            var rawShorts = BuildRungs(newCenter, OrderSide.Short, _cfg.BaseRungsPerSide, spacing);

            var longRungs = rawLongs.Select(r => r with { Price = _quantizePrice(r.Price) }).ToList();
            var shortRungs = rawShorts.Select(r => r with { Price = _quantizePrice(r.Price) }).ToList();

            // place LONG entries above center
            for (int i = 0; i < longRungs.Count; i++)
            {
                var r = longRungs[i];
                var tp = _quantizePrice(r.Price * (1 + _cfg.TakeProfitPct) + _cfg.FundingRateAdjustment);
                var sl = _quantizePrice(r.Price * (1 - _cfg.StopLossPct) - _cfg.FundingRateAdjustment);
                var id = await _orders.PlaceLimitOrderAsync(_symbol, r.Price, OrderSide.Long, tp, sl);
                longRungs[i] = r with { OrderId = id };
                Console.WriteLine($"  ↳ LONG  @ {r.Price:F6} → TP {tp:F6}, SL {sl:F6} → {id}");
            }

            // place SHORT entries below center
            for (int i = 0; i < shortRungs.Count; i++)
            {
                var r = shortRungs[i];
                var tp = _quantizePrice(r.Price * (1 - _cfg.TakeProfitPct) - _cfg.FundingRateAdjustment);
                var sl = _quantizePrice(r.Price * (1 + _cfg.StopLossPct) + _cfg.FundingRateAdjustment);
                var id = await _orders.PlaceLimitOrderAsync(_symbol, r.Price, OrderSide.Short, tp, sl);
                shortRungs[i] = r with { OrderId = id };
                Console.WriteLine($"  ↳ SHORT @ {r.Price:F6} → TP {tp:F6}, SL {sl:F6} → {id}");
            }

            _rungs[OrderSide.Long] = longRungs;
            _rungs[OrderSide.Short] = shortRungs;

            Console.WriteLine($"\n[{_symbol}] Ladder shifted: {longRungs.Count} longs above, {shortRungs.Count} shorts below.\n");
            OnRungsUpdated?.Invoke(longRungs.Concat(shortRungs));
        }

        private static List<Rung> BuildRungs(decimal center, OrderSide side, int count, decimal spacing)
        {
            var list = new List<Rung>();
            for (int i = 1; i <= count; i++)
            {
                var price = side == OrderSide.Long
                    ? center + spacing * i
                    : center - spacing * i;
                list.Add(new Rung(price, side));
            }
            return list;
        }
    }
}
