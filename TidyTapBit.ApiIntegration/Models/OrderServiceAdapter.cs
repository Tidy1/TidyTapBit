using Newtonsoft.Json;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

using TidyTrader.ApiIntegration.LadderStrategy;
using TidyTrader.ApiIntegration.Models;

public class OrderServiceAdapter : IOrderService
{
    private readonly BitunixApiClient _api;
    private readonly LiveCapitalManager _cap;
    private readonly decimal _usdPerOrder;
    private readonly Dictionary<string, decimal> _tpPrices = new();
    private readonly HashSet<string> _tpFills = new();

    public OrderServiceAdapter(
        BitunixApiClient api,
        LiveCapitalManager cap,
        decimal usdPerOrder = 5m        // ← hard-coded $5 per trade
    )
    {
        _api = api;
        _cap = cap;
        _usdPerOrder = usdPerOrder;
    }

    public async Task<string?> PlaceLimitOrderAsync(string symbol, decimal price, OrderSide side, decimal tpPrice, decimal slPrice)
    {
        // 1) How much USDT margin per order
        decimal marginUsd = 5m;

        // 2) Your leverage
        decimal leverage = 25m;

        // 3) Calculate notional size and then qty
        //    notional = marginUsd * leverage
        //    qty      = notional / price
        decimal notional = marginUsd * leverage;
        decimal qty = Math.Round(notional / price, 4);
        if (qty <= 0)
        {
            Console.WriteLine($"[ERROR] Qty too small for {symbol} @ {price:F4}: qty={qty}");
            return null;
        }

        // 4) Compute actual margin that will be reserved (should equal marginUsd)
        decimal actualMargin = (qty * price) / leverage;

        // 5) Check you have that much available
        if (_cap.Available < actualMargin)
        {
            Console.WriteLine($"[SKIP] {symbol}: available capital ${_cap.Available:F2} < required margin ${actualMargin:F2}");
            return null;
        }

        // 6) Place the REST order
        var resp = await _api.PlaceOrderAsync(
            symbol,
            qty.ToString(CultureInfo.InvariantCulture),
            side.ToString().ToUpper(), "OPEN", "LIMIT",
            price.ToString(CultureInfo.InvariantCulture),
            "GTC", Guid.NewGuid().ToString(),
            null, false,
            tpPrice.ToString(CultureInfo.InvariantCulture), "MARK_PRICE", "LIMIT",
            tpPrice.ToString(CultureInfo.InvariantCulture),
            slPrice.ToString(CultureInfo.InvariantCulture), "MARK_PRICE", "LIMIT",
            slPrice.ToString(CultureInfo.InvariantCulture)
        );

        // 7) Log the raw response
        Console.WriteLine($"[API] PlaceOrderAsync → HTTP {(int)resp.StatusCode} {resp.StatusCode}");
        Console.WriteLine($"[API][Body] {JsonConvert.SerializeObject(resp.Data)}");

        // 8) If we got an orderId, reserve margin
        var id = resp?.Data?.Data?.OrderId;
        if (id != null)
        {
            try
            {
                _cap.ReserveMargin(id, actualMargin);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CAPITAL‐ERROR] Could not reserve ${actualMargin:F2} for {id}: {ex.Message}");
                await _api.CancelOrdersAsync(symbol, new[] { id });
                return null;
            }
            Console.WriteLine($"[ORDER] {symbol} {side} @ {price:F4} qty={qty} TP={tpPrice:F4} SL={slPrice:F4} → {id}");
        }
        else
        {
            Console.WriteLine($"[ERROR] Order failed for {symbol} {side} @ {price:F4}");
        }

        return id;
    }



    public async Task CancelOrdersAsync(IEnumerable<string> orderIds)
    {
        // (your existing REST cancel logic, e.g. batch‐cancel)
        await _api.CancelOrdersAsync(
            orderIds.First().Substring(0, orderIds.First().IndexOfAny("0123456789".ToCharArray())),
            orderIds.ToList()
        );

        // release all those margins
        foreach (var id in orderIds)
            _cap.ReleaseMargin(id);
    }

    public bool WasTakeProfitFill(string orderId)
    {
        // return true once, if this order was marked as a TP
        return _tpFills.Remove(orderId);
    }

    /// <summary>
    /// Call *before* you call ladder.OnOrderFilledAsync(...)
    /// </summary>
    public void NotifyFill(string orderId, decimal fillPrice)
    {
        if (_tpPrices.TryGetValue(orderId, out var expectedTp))
        {
            const decimal EPS = 0.0000001m;
            if (Math.Abs(fillPrice - expectedTp) <= EPS)
            {
                // this really was a TP fill
                _tpFills.Add(orderId);
            }

            // whether or not it matched TP, we don't need to keep the price anymore
            _tpPrices.Remove(orderId);
        }
    }
}