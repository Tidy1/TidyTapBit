using RestSharp;

using System.Security.Cryptography;
using System.Text;

using TidyTrader.ApiIntegration.Interfaces;

namespace TidyTrader.ApiIntegration.Models
{
    public class BitunixApiClient : IBitunixApiClient
    {
        private readonly string _apiKey;
        private readonly string _apiSecret;
        private readonly string _baseUrl = "https://fapi.bitunix.com";

        public BitunixApiClient(string apiKey, string apiSecret)
        {
            _apiKey = apiKey;
            _apiSecret = apiSecret;
        }

        /// <summary>
        /// Cancels all orders for a symbol.
        /// </summary>
        /// <param name="symbol">The symbol.</param>
        /// <returns>The response as a string.</returns>
        public async Task<string> CancelAllOrdersAsync(string symbol)
        {
            var body = new { symbol };
            return await SendRequestAsync("POST", "/api/v1/futures/trade/cancel_all_orders", body);
        }

        /// <summary>
        /// Cancels specific orders for a symbol.
        /// </summary>
        /// <param name="symbol">The symbol.</param>
        /// <param name="orderIds">The order IDs.</param>
        /// <returns>The response as a string.</returns>
        public async Task<string> CancelOrdersAsync(string symbol, string[] orderIds)
        {
            var body = new { symbol, orderIds };
            return await SendRequestAsync("POST", "/api/v1/futures/trade/cancel_orders", body);
        }

        /// <summary>
        /// Cancels a TP/SL order.
        /// </summary>
        /// <param name="symbol">The symbol.</param>
        /// <param name="orderId">The order ID.</param>
        /// <returns>The response as a string.</returns>
        public async Task<string> CancelTpSlOrderAsync(string symbol, string orderId)
        {
            var body = new { symbol, orderId };
            return await SendRequestAsync("POST", "/api/v1/futures/tpsl/cancel_order", body);
        }

        /// <summary>
        /// Gets the account information.
        /// </summary>
        /// <param name="marginCoin">The margin coin.</param>
        /// <returns>The account information as a string.</returns>
        public async Task<string> GetAccountInfoAsync(string marginCoin)
        {
            return await SendRequestAsync("GET", $"/api/v1/futures/account?marginCoin={marginCoin}");
        }

        /// <summary>
        /// Gets the funding rate.
        /// </summary>
        /// <param name="symbol">The symbol.</param>
        /// <returns>The funding rate as a string.</returns>
        public async Task<string> GetFundingRateAsync(string symbol)
        {
            return await SendRequestAsync("GET", $"/api/v1/futures/market/funding_rate?symbol={symbol}");
        }

        /// <summary>
        /// Gets the history positions.
        /// </summary>
        /// <param name="symbol">The symbol.</param>
        /// <param name="limit">The limit.</param>
        /// <returns>The history positions as a string.</returns>
        public async Task<string> GetHistoryPositionsAsync(string symbol, int limit)
        {
            return await SendRequestAsync("GET", $"/api/v1/futures/position/get_history_positions?symbol={symbol}&limit={limit}");
        }

        /// <summary>
        /// Gets the history TP/SL orders.
        /// </summary>
        /// <param name="symbol">The symbol.</param>
        /// <returns>The history TP/SL orders as a string.</returns>
        public async Task<string> GetHistoryTpSlOrderAsync(string symbol)
        {
            return await SendRequestAsync("GET", $"/api/v1/futures/tpsl/get_historys_order?symbol={symbol}");
        }

        /// <summary>
        /// Gets the history trades.
        /// </summary>
        /// <param name="symbol">The symbol (optional).</param>
        /// <param name="startTime">The start time (optional).</param>
        /// <param name="endTime">The end time (optional).</param>
        /// <param name="limit">The limit (optional).</param>
        /// <returns>The history trades as a string.</returns>
        public async Task<string> GetHistoryTradesAsync(string? symbol = null, long? startTime = null, long? endTime = null, int? limit = null)
        {
            var path = "/api/v1/futures/trade/get_history_trades";
            var queryParams = new List<string>();
            if (!string.IsNullOrEmpty(symbol))
            {
                queryParams.Add($"symbol={symbol}");
            }
            if (startTime.HasValue)
            {
                queryParams.Add($"startTime={startTime}");
            }
            if (endTime.HasValue)
            {
                queryParams.Add($"endTime={endTime}");
            }
            if (limit.HasValue)
            {
                queryParams.Add($"limit={limit}");
            }
            if (queryParams.Any())
            {
                path += "?" + string.Join("&", queryParams);
            }
            return await SendRequestAsync("GET", path);
        }

        /// <summary>
        /// Gets the Kline data.
        /// </summary>
        /// <param name="symbol">The symbol.</param>
        /// <param name="interval">The interval.</param>
        /// <param name="limit">The limit.</param>
        /// <returns>The Kline data as a string.</returns>
        public async Task<string> GetKlineAsync(string symbol, string interval, int limit)
        {
            return await SendRequestAsync("GET", $"/api/v1/futures/market/kline?symbol={symbol}&interval={interval}&limit={limit}");
        }

        /// <summary>
        /// Gets the market depth.
        /// </summary>
        /// <param name="symbol">The symbol.</param>
        /// <param name="limit">The limit (default is "max").</param>
        /// <returns>The market depth as a string.</returns>
        public async Task<string> GetMarketDepthAsync(string symbol, string limit = "max")
        {
            return await SendRequestAsync("GET", $"/api/v1/futures/market/depth?symbol={symbol}&limit={limit}");
        }

        /// <summary>
        /// Gets the market tickers.
        /// </summary>
        /// <param name="symbol">The symbol (optional).</param>
        /// <returns>The market tickers as a string.</returns>
        public async Task<string> GetMarketTickersAsync(string? symbol = null)
        {
            var path = "/api/v1/futures/market/tickers";
            if (!string.IsNullOrEmpty(symbol))
            {
                path += $"?symbol={symbol}";
            }
            return await SendRequestAsync("GET", path);
        }

        /// <summary>
        /// Gets the pending positions.
        /// </summary>
        /// <param name="symbol">The symbol.</param>
        /// <returns>The pending positions as a string.</returns>
        public async Task<string> GetPendingPositionsAsync(string symbol)
        {
            return await SendRequestAsync("GET", $"/api/v1/futures/position/get_pending_positions?symbol={symbol}");
        }

        /// <summary>
        /// Gets the pending TP/SL orders.
        /// </summary>
        /// <param name="symbol">The symbol.</param>
        /// <returns>The pending TP/SL orders as a string.</returns>
        public async Task<string> GetPendingTpSlOrderAsync(string symbol)
        {
            return await SendRequestAsync("GET", $"/api/v1/futures/tpsl/get_pending_orders?symbol={symbol}");
        }

        /// <summary>
        /// Gets the position tiers.
        /// </summary>
        /// <param name="symbol">The symbol.</param>
        /// <returns>The position tiers as a string.</returns>
        public async Task<string> GetPositionTiersAsync(string symbol)
        {
            return await SendRequestAsync("GET", $"/api/v1/futures/position/position_tiers?symbol={symbol}");
        }

        /// <summary>
        /// Gets the server time.
        /// </summary>
        /// <returns>The server time as a string.</returns>
        public async Task<string> GetServerTimeAsync()
        {
            return await SendRequestAsync("GET", "/api/v1/futures/market/time");
        }

        /// <summary>
        /// Gets the trading pairs.
        /// </summary>
        /// <returns>The trading pairs as a string.</returns>
        public async Task<string> GetTradingPairsAsync()
        {
            return await SendRequestAsync("GET", "/api/v1/futures/market/trading_pairs");
        }

        /// <summary>
        /// Modifies a position TP/SL order.
        /// </summary>
        /// <param name="symbol">The symbol.</param>
        /// <param name="orderId">The order ID.</param>
        /// <param name="stopLoss">The stop loss.</param>
        /// <param name="takeProfit">The take profit.</param>
        /// <returns>The response as a string.</returns>
        public async Task<string> ModifyPositionTpSlOrderAsync(string symbol, string orderId, decimal stopLoss, decimal takeProfit)
        {
            var body = new { symbol, orderId, stopLoss, takeProfit };
            return await SendRequestAsync("POST", "/api/v1/futures/tpsl/position/modify_order", body);
        }

        /// <summary>
        /// Modifies a TP/SL order.
        /// </summary>
        /// <param name="symbol">The symbol.</param>
        /// <param name="orderId">The order ID.</param>
        /// <param name="stopLoss">The stop loss.</param>
        /// <param name="takeProfit">The take profit.</param>
        /// <returns>The response as a string.</returns>
        public async Task<string> ModifyTpSlOrderAsync(string symbol, string orderId, decimal stopLoss, decimal takeProfit)
        {
            var body = new { symbol, orderId, stopLoss, takeProfit };
            return await SendRequestAsync("POST", "/api/v1/futures/tpsl/modify_order", body);
        }

        /// <summary>
        /// Places a futures order.
        /// </summary>
        /// <param name="symbol">The symbol.</param>
        /// <param name="qty">The quantity.</param>
        /// <param name="side">The side (buy/sell).</param>
        /// <param name="orderType">The order type (limit/market).</param>
        /// <param name="leverage">The leverage.</param>
        /// <param name="price">The price (optional).</param>
        /// <param name="clientOrderId">The client order ID (optional).</param>
        /// <param name="timeInForce">The time in force (optional).</param>
        /// <param name="reduceOnly">Whether the order is reduce-only (optional).</param>
        /// <param name="postOnly">Whether the order is post-only (optional).</param>
        /// <param name="positionSide">The position side (optional).</param>
        /// <returns>The response as a string.</returns>
        public async Task<string> PlaceFuturesOrderAsync(string symbol, decimal qty, string side, string orderType, int leverage, decimal? price = null, string clientOrderId = null, string timeInForce = null, bool? reduceOnly = null, bool? postOnly = null, string positionSide = null)
        {
            var body = new
            {
                symbol,
                qty,
                side,
                orderType,
                leverage,
                price,
                clientOrderId,
                timeInForce,
                reduceOnly,
                postOnly,
                positionSide,
                marginMode = "isolated"
            };
            return await SendRequestAsync("POST", "/api/v1/futures/trade/place_order", body);
        }

        /// <summary>
        /// Places a position TP/SL order.
        /// </summary>
        /// <param name="symbol">The symbol.</param>
        /// <param name="stopLoss">The stop loss.</param>
        /// <param name="takeProfit">The take profit.</param>
        /// <returns>The response as a string.</returns>
        public async Task<string> PlacePositionTpSlOrderAsync(string symbol, decimal stopLoss, decimal takeProfit)
        {
            var body = new { symbol, stopLoss, takeProfit };
            return await SendRequestAsync("POST", "/api/v1/futures/tpsl/position/place_order", body);
        }

        /// <summary>
        /// Places a TP/SL order.
        /// </summary>
        /// <param name="symbol">The symbol.</param>
        /// <param name="stopLoss">The stop loss.</param>
        /// <param name="takeProfit">The take profit.</param>
        /// <returns>The response as a string.</returns>
        public async Task<string> PlaceTpSlOrderAsync(string symbol, decimal stopLoss, decimal takeProfit)
        {
            var body = new { symbol, stopLoss, takeProfit };
            return await SendRequestAsync("POST", "/api/v1/futures/tpsl/place_order", body);
        }

        private string GenerateSignature(string timestamp, string nonce, string method, string path, string body, string queryParams = "")
        {
            // Step 1: Construct the digest input
            string digestInput = nonce + timestamp + _apiKey + queryParams + body;

            // Step 2: Generate the first SHA256 hash (digest)
            string digest;
            using (var sha256 = SHA256.Create())
            {
                byte[] digestBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(digestInput));
                digest = BitConverter.ToString(digestBytes).Replace("-", "").ToLower();
            }

            // Step 3: Construct the sign input by appending the secret key
            string signInput = digest + _apiSecret;

            // Step 4: Generate the second SHA256 hash (sign)
            string sign;
            using (var sha256 = SHA256.Create())
            {
                byte[] signBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(signInput));
                sign = BitConverter.ToString(signBytes).Replace("-", "").ToLower();
            }

            return sign;
        }

        private async Task<string> SendRequestAsync(string method, string path, object body = null)
        {
            var client = new RestClient(_baseUrl);
            var request = new RestRequest(path, method == "GET" ? Method.Get : Method.Post);

            // Generate timestamp and nonce
            string timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
            string nonce = Guid.NewGuid().ToString("N").Substring(0, 32);

            // Serialize the body to JSON if it exists
            string bodyJson = body != null ? Newtonsoft.Json.JsonConvert.SerializeObject(body) : "";

            // Generate the signature
            string signature = GenerateSignature(timestamp, nonce, method, path, bodyJson);

            // Add required headers
            request.AddHeader("api-key", _apiKey);
            request.AddHeader("sign", signature);
            request.AddHeader("nonce", nonce);
            request.AddHeader("timestamp", timestamp);            
            request.AddHeader("language", "en-US");
            request.AddHeader("Content-Type", "application/json");

            // Add the body to the request if it exists
            if (body != null)
            {
                request.AddJsonBody(bodyJson);
            }

            // Execute the request and handle the response
            var response = await client.ExecuteAsync(request);

            // Check for HTTP errors
            if (!response.IsSuccessful)
            {
                throw new HttpRequestException($"Request failed with status code {response.StatusCode}: {response.Content}");
            }

            return response.Content;
        }

    }
}