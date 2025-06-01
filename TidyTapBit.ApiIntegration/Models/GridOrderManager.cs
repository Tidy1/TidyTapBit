using Skender.Stock.Indicators;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;

using TidyTrader.ApiIntegration.Interfaces;
using TidyTrader.ApiIntegration.Models.Responses.Trade;


namespace TidyTrader.ApiIntegration.Models
{
    public class GridOrderManager
    {
        private readonly BitunixApiClient _client;
        private readonly ConcurrentDictionary<string, GridOrder> _activeOrders = new();
        private readonly CapitalManager _capitalManager;

        public GridOrderManager(BitunixApiClient client, CapitalManager capitalManager)
        {
            _client = client;
            _capitalManager = capitalManager;
        }

        public async Task<List<ApiResponse<PlaceOrderResponse>>> PlaceGridOrdersAsync(
            string symbol,
            string marginCoin,
            decimal currentPrice,
            GridConfig config,
            bool isBullish)
        {
            var responses = new List<ApiResponse<PlaceOrderResponse>>();

            int longCount = isBullish ? config.LongOrderCount + 1 : config.LongOrderCount;
            int shortCount = isBullish ? config.ShortOrderCount : config.ShortOrderCount + 1;

            for (int i = 1; i <= longCount; i++)
            {
                decimal entry = currentPrice * (1 - (config.PriceSpacingPct * i));
                decimal capitalRequired = entry * config.OrderQuantity / config.Leverage;
                if (!_capitalManager.TryReserve(capitalRequired)) break;

                string clientId = Guid.NewGuid().ToString();
                var response = await _client.PlaceOrderAsync(
                    symbol: symbol,
                    qty: config.OrderQuantity.ToString(CultureInfo.InvariantCulture),
                    side: "BUY",
                    tradeSide: "OPEN",
                    orderType: "LIMIT",
                    price: entry.ToString(CultureInfo.InvariantCulture),
                    effect: "GTC",
                    clientId: clientId,
                    reduceOnly: false,
                    tpPrice: (entry * (1 + config.TakeProfitPct)).ToString(CultureInfo.InvariantCulture),
                    tpStopType: "MARK_PRICE",
                    tpOrderType: "LIMIT",
                    tpOrderPrice: (entry * (1 + config.TakeProfitPct)).ToString(CultureInfo.InvariantCulture),
                    slPrice: (entry * (1 - config.StopLossPct)).ToString(CultureInfo.InvariantCulture),
                    slStopType: "MARK_PRICE",
                    slOrderType: "LIMIT",
                    slOrderPrice: (entry * (1 - config.StopLossPct)).ToString(CultureInfo.InvariantCulture)
                );

                if (response.Data.Data.OrderId != null)
                    _activeOrders[clientId] = new GridOrder(clientId, capitalRequired);

                responses.Add(response);
            }

            for (int i = 1; i <= shortCount; i++)
            {
                decimal entry = currentPrice * (1 + (config.PriceSpacingPct * i));
                decimal capitalRequired = entry * config.OrderQuantity / config.Leverage;
                if (!_capitalManager.TryReserve(capitalRequired)) break;

                string clientId = Guid.NewGuid().ToString();
                var response = await _client.PlaceOrderAsync(
                    symbol: symbol,
                    qty: config.OrderQuantity.ToString(CultureInfo.InvariantCulture),
                    side: "SELL",
                    tradeSide: "OPEN",
                    orderType: "LIMIT",
                    price: entry.ToString(CultureInfo.InvariantCulture),
                    effect: "GTC",
                    clientId: clientId,
                    reduceOnly: false,
                    tpPrice: (entry * (1 - config.TakeProfitPct)).ToString(CultureInfo.InvariantCulture),
                    tpStopType: "MARK_PRICE",
                    tpOrderType: "LIMIT",
                    tpOrderPrice: (entry * (1 - config.TakeProfitPct)).ToString(CultureInfo.InvariantCulture),
                    slPrice: (entry * (1 + config.StopLossPct)).ToString(CultureInfo.InvariantCulture),
                    slStopType: "MARK_PRICE",
                    slOrderType: "LIMIT",
                    slOrderPrice: (entry * (1 + config.StopLossPct)).ToString(CultureInfo.InvariantCulture)
                );

                if (response.Data.Data.OrderId != null)
                    _activeOrders[clientId] = new GridOrder(clientId, capitalRequired);

                responses.Add(response);
            }

            return responses;
        }

        public async Task SyncOpenOrdersAsync(string symbol)
        {
            var response = await _client.GetPendingOrdersAsync(symbol);
            if (response.Data.Data.OrderList == null) return;

            foreach (var order in response.Data.Data.OrderList)
            {
                if (!_activeOrders.ContainsKey(order.ClientId))
                {
                    _activeOrders[order.ClientId] = new GridOrder(order.ClientId, 0); // Capital unknown, assume accounted
                }
            }
        }

        public void HandleOrderClosed(string clientId)
        {
            if (_activeOrders.TryRemove(clientId, out var order))
            {
                _capitalManager.Release(order.CapitalReserved);
            }
        }

        public async Task CloseAllPositionsAsync(string symbol)
        {
            await _client.CloseAllPositionsAsync(symbol);
        }
    }

    public class GridConfig
    {
        public int LongOrderCount { get; set; } = 3;
        public int ShortOrderCount { get; set; } = 3;
        public decimal PriceSpacingPct { get; set; } = 0.0001m; // 0.01%
        public decimal TakeProfitPct { get; set; } = 0.0002m;    // 0.02%
        public decimal StopLossPct { get; set; } = 0.00008m;     // 0.008%
        public decimal Leverage { get; set; } = 100;
        public decimal OrderQuantity { get; set; } = 0.01m;      // contract size
    }

    public class GridOrder
    {
        public string ClientId { get; }
        public decimal CapitalReserved { get; }

        public GridOrder(string clientId, decimal capitalReserved)
        {
            ClientId = clientId;
            CapitalReserved = capitalReserved;
        }
    }

    public class CapitalManager
    {
        private decimal _totalCapital;
        private decimal _allocatedCapital;

        public CapitalManager(decimal totalCapital)
        {
            _totalCapital = totalCapital;
        }

        public bool TryReserve(decimal amount)
        {
            if (_allocatedCapital + amount > _totalCapital) return false;
            _allocatedCapital += amount;
            return true;
        }

        public void Release(decimal amount)
        {
            _allocatedCapital -= amount;
            if (_allocatedCapital < 0) _allocatedCapital = 0;
        }

        public decimal Available => _totalCapital - _allocatedCapital;
        public decimal Allocated => _allocatedCapital;
    }

}