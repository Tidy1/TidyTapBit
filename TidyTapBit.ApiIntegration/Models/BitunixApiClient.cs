using Newtonsoft.Json;

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

        private async Task<RestResponse> SendRequestAsync(string method, string path, string queryParams = "", object body = null)
        {
            var client = new RestClient(_baseUrl);
            var fullPath = path + (string.IsNullOrWhiteSpace(queryParams) ? "" : $"?{queryParams}");
            var request = new RestRequest(fullPath, method == "GET" ? Method.Get : Method.Post);

            // Generate required fields
            string timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();


            string nonce = Guid.NewGuid().ToString("N").Substring(0, 32);

            string compactBody = (body != null && method != "GET")
             ? JsonConvert.SerializeObject(body, Formatting.None)
             : "";

            // Sort and clean query params
            string sortedQueryParams = FlattenQueryParams(queryParams);

            // Create digest and final signature
            string digestInput = $"{nonce}{timestamp}{_apiKey}{sortedQueryParams}{compactBody}";
            string digest = Sha256Hex(digestInput.Trim());
            string signature = Sha256Hex(digest + _apiSecret);

            #region Debug
            // Log for debug
            //Console.WriteLine("=== Bitunix Signature Debug ===");
            //Console.WriteLine($"Method: {method}");
            //Console.WriteLine($"Path: {path}");
            //Console.WriteLine($"QueryParams: {queryParams}");
            //Console.WriteLine($"SortedParams: {sortedQueryParams}");
            //Console.WriteLine($"Digest Input: {digestInput}");
            //Console.WriteLine($"Digest: {digest}");
            //Console.WriteLine($"Signature Input: {digest + _apiSecret}");
            //Console.WriteLine($"Signature: {signature}");
            //Console.WriteLine($"Full Path: {fullPath}");
            //Console.WriteLine($"Request Body: {(method == "GET" ? "(not sent)" : compactBody)}");
            //Console.WriteLine("================================");
            #endregion

            // Headers (in correct order)
            request.AddOrUpdateHeader("api-key", _apiKey);
            request.AddOrUpdateHeader("sign", signature);
            request.AddOrUpdateHeader("nonce", nonce);
            request.AddOrUpdateHeader("timestamp", timestamp);
            request.AddOrUpdateHeader("language", "en-US");
            request.AddOrUpdateHeader("Content-Type", "application/json");

            // Only add body for POST/PUT
            if (method != "GET" && !string.IsNullOrEmpty(compactBody))
            {
                request.AddStringBody(compactBody, DataFormat.Json);
            }

            var response = await client.ExecuteAsync(request);

            if (!response.IsSuccessful)
            {
                throw new HttpRequestException($"Request failed [{response.StatusCode}]: {response.Content}");
            }

            return response;
        }

        private static string SortQueryParams(string queryParams)
        {
            if (string.IsNullOrWhiteSpace(queryParams)) return "";

            return string.Join("&", queryParams
                .Split('&', StringSplitOptions.RemoveEmptyEntries)
                .Select(kv =>
                {
                    var parts = kv.Split('=', 2);
                    return new KeyValuePair<string, string>(parts[0], parts.Length > 1 ? parts[1] : "");
                })
                .OrderBy(kv => kv.Key, StringComparer.Ordinal)
                .Select(kv => $"{kv.Key}={kv.Value}"));
        }

        private static string FlattenQueryParams(string queryParams)
        {
            if (string.IsNullOrWhiteSpace(queryParams)) return "";

            return string.Concat(
                queryParams
                .Split('&', StringSplitOptions.RemoveEmptyEntries)
                .Select(kv =>
                {
                    var parts = kv.Split('=', 2);
                    var key = parts[0];
                    var value = parts.Length > 1 ? parts[1] : "";
                    return key + value;
                })
                .OrderBy(s => s, StringComparer.Ordinal)
            );
        }

        private static string Sha256Hex(string input)
        {
            using var sha256 = SHA256.Create();
            var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
            return BitConverter.ToString(hash).Replace("-", "").ToLower();
        }


        #region Account

        public async Task<RestResponse> GetAccountInfoAsync(string marginCoin)
        {
            var queryParams = $"marginCoin={marginCoin}";
            var path = "/api/v1/futures/account";

            return await SendRequestAsync("GET", path, queryParams, null);
        }

        public async Task<RestResponse> AdjustPositionMarginAsync(string marginCoin, string symbol, decimal amount, int type)
        {
            var path = "/api/v1/futures/account/adjust_margin";

            var bodyObj = new
            {
                marginCoin = marginCoin,
                symbol = symbol,
                amount = amount.ToString("0.########"), // Ensure proper formatting
                type = type
            };

            string bodyJson = JsonConvert.SerializeObject(bodyObj, Formatting.None);

            return await SendRequestAsync("POST", path, "", bodyJson);
        }

        public async Task<RestResponse> ChangeLeverageAsync(string symbol, string marginCoin, int leverage)
        {
            var path = "/api/v1/futures/account/set_leverage";

            var bodyObj = new
            {
                symbol = symbol,
                marginCoin = marginCoin,
                leverage = leverage
            };

            string bodyJson = JsonConvert.SerializeObject(bodyObj, Formatting.None);

            return await SendRequestAsync("POST", path, "", bodyJson);
        }

        public async Task<RestResponse> ChangeMarginModeAsync(string symbol, string marginCoin, string marginMode)
        {
            var path = "/api/v1/futures/account/set_margin_mode";

            var bodyObj = new
            {
                symbol = symbol,
                marginCoin = marginCoin,
                marginMode = marginMode
            };

            string bodyJson = JsonConvert.SerializeObject(bodyObj, Formatting.None);

            return await SendRequestAsync("POST", path, "", bodyJson);
        }

        /// <summary>
        /// await apiClient.ChangePositionModeAsync("BTCUSDT", "USDT", 1); // One-way mode
        //  await apiClient.ChangePositionModeAsync("BTCUSDT", "USDT", 2); // Hedge mode
        /// </summary>
        /// <param name="symbol"></param>
        /// <param name="marginCoin"></param>
        /// <param name="positionMode"></param>
        /// <returns></returns>
        public async Task<RestResponse> ChangePositionModeAsync(string symbol, string marginCoin, int positionMode)
        {
            var path = "/api/v1/futures/account/set_position_mode";

            var bodyObj = new
            {
                symbol = symbol,
                marginCoin = marginCoin,
                positionMode = positionMode
            };

            string bodyJson = JsonConvert.SerializeObject(bodyObj, Formatting.None);

            return await SendRequestAsync("POST", path, "", bodyJson);
        }

        public async Task<RestResponse> GetLeverageAndMarginModeAsync(string symbol, string marginCoin)
        {
            var path = "/api/v1/futures/account/get_leverage_and_margin_mode";
            var queryParams = $"symbol={symbol}&marginCoin={marginCoin}";

            return await SendRequestAsync("GET", path, queryParams, null);
        }

        public async Task<RestResponse> GetSingleAccountAsync(string symbol, string marginCoin)
        {
            var path = "/api/v1/futures/account";
            var queryParams = $"symbol={symbol}&marginCoin={marginCoin}";

            return await SendRequestAsync("GET", path, queryParams, null);
        }

        #endregion

        #region Trade

        public async Task<RestResponse> PlaceBatchOrdersAsync(string symbol, string marginCoin, IEnumerable<object> orderDataList)
        {
            var path = "/api/v1/futures/trade/batch_order";

            var bodyObj = new
            {
                symbol = symbol,
                marginCoin = marginCoin,
                orderDataList = orderDataList
            };

            string bodyJson = JsonConvert.SerializeObject(bodyObj, Formatting.None);

            return await SendRequestAsync("POST", path, "", bodyJson);
        }

        public async Task<RestResponse> CancelAllOrdersAsync(string symbol)
        {
            var path = "/api/v1/futures/trade/cancel_all_orders";

            // Prepare body JSON, Bitunix requires POST with body = { "symbol": "BTCUSDT" }
            var bodyObj = new { symbol = symbol };
            string bodyJson = JsonConvert.SerializeObject(bodyObj, Formatting.None);

            // No query params for this endpoint
            return await SendRequestAsync("POST", path, "", bodyJson);
        }

        public async Task<RestResponse> CancelOrdersAsync(string symbol, IEnumerable<string> orderIds)
        {
            var path = "/api/v1/futures/trade/cancel_orders";

            var bodyObj = new
            {
                symbol = symbol,
                orderIds = orderIds
            };

            string bodyJson = JsonConvert.SerializeObject(bodyObj, Formatting.None);

            return await SendRequestAsync("POST", path, "", bodyJson);
        }

        public async Task<RestResponse> CloseAllPositionsAsync(string symbol)
        {
            var path = "/api/v1/futures/trade/close_all_position";

            var bodyObj = new
            {
                symbol = symbol
            };

            string bodyJson = JsonConvert.SerializeObject(bodyObj, Formatting.None);

            return await SendRequestAsync("POST", path, "", bodyJson);
        }

        public async Task<RestResponse> FlashClosePositionAsync(string positionId)
        {
            var path = "/api/v1/futures/trade/flash_close_position";

            var bodyObj = new
            {
                positionId = positionId
            };

            string bodyJson = JsonConvert.SerializeObject(bodyObj, Formatting.None);

            return await SendRequestAsync("POST", path, "", bodyJson);
        }

        public async Task<RestResponse> GetHistoryOrdersAsync(string symbol = null, string orderId = null, string clientId = null, string status = null, string type = null, long? startTime = null, long? endTime = null, long? skip = null, long? limit = null)
        {
            var path = "/api/v1/futures/trade/get_history_orders";

            var queryParams = new List<string>();

            if (!string.IsNullOrWhiteSpace(symbol)) queryParams.Add($"symbol={symbol}");
            if (!string.IsNullOrWhiteSpace(orderId)) queryParams.Add($"orderId={orderId}");
            if (!string.IsNullOrWhiteSpace(clientId)) queryParams.Add($"clientId={clientId}");
            if (!string.IsNullOrWhiteSpace(status)) queryParams.Add($"status={status}");
            if (!string.IsNullOrWhiteSpace(type)) queryParams.Add($"type={type}");
            if (startTime.HasValue) queryParams.Add($"startTime={startTime}");
            if (endTime.HasValue) queryParams.Add($"endTime={endTime}");
            if (skip.HasValue) queryParams.Add($"skip={skip}");
            if (limit.HasValue) queryParams.Add($"limit={limit}");

            string queryString = string.Join("&", queryParams);

            return await SendRequestAsync("GET", path, queryString, null);
        }

        public async Task<RestResponse> GetHistoryTradesAsync(string symbol = null, string orderId = null, string positionId = null, long? startTime = null, long? endTime = null, long? skip = null, long? limit = null)
        {
            var path = "/api/v1/futures/trade/get_history_trades";

            var queryParams = new List<string>();

            if (!string.IsNullOrWhiteSpace(symbol)) queryParams.Add($"symbol={symbol}");
            if (!string.IsNullOrWhiteSpace(orderId)) queryParams.Add($"orderId={orderId}");
            if (!string.IsNullOrWhiteSpace(positionId)) queryParams.Add($"positionId={positionId}");
            if (startTime.HasValue) queryParams.Add($"startTime={startTime}");
            if (endTime.HasValue) queryParams.Add($"endTime={endTime}");
            if (skip.HasValue) queryParams.Add($"skip={skip}");
            if (limit.HasValue) queryParams.Add($"limit={limit}");

            string queryString = string.Join("&", queryParams);

            return await SendRequestAsync("GET", path, queryString, null);
        }

        public async Task<RestResponse> GetOrderDetailAsync(string orderId = null, string clientId = null)
        {
            if (string.IsNullOrWhiteSpace(orderId) && string.IsNullOrWhiteSpace(clientId))
                throw new ArgumentException("Either orderId or clientId must be provided.");

            var path = "/api/v1/futures/trade/get_order_detail";

            var queryParams = new List<string>();
            if (!string.IsNullOrWhiteSpace(orderId)) queryParams.Add($"orderId={orderId}");
            if (!string.IsNullOrWhiteSpace(clientId)) queryParams.Add($"clientId={clientId}");

            string queryString = string.Join("&", queryParams);

            return await SendRequestAsync("GET", path, queryString, null);
        }

        public async Task<RestResponse> GetPendingOrdersAsync(string symbol = null, string orderId = null, string clientId = null, string status = null, long? startTime = null, long? endTime = null, long? skip = null, long? limit = null)
        {
            var path = "/api/v1/futures/trade/get_pending_orders";

            var queryParams = new List<string>();

            if (!string.IsNullOrWhiteSpace(symbol)) queryParams.Add($"symbol={symbol}");
            if (!string.IsNullOrWhiteSpace(orderId)) queryParams.Add($"orderId={orderId}");
            if (!string.IsNullOrWhiteSpace(clientId)) queryParams.Add($"clientId={clientId}");
            if (!string.IsNullOrWhiteSpace(status)) queryParams.Add($"status={status}");
            if (startTime.HasValue) queryParams.Add($"startTime={startTime}");
            if (endTime.HasValue) queryParams.Add($"endTime={endTime}");
            if (skip.HasValue) queryParams.Add($"skip={skip}");
            if (limit.HasValue) queryParams.Add($"limit={limit}");

            string queryString = string.Join("&", queryParams);

            return await SendRequestAsync("GET", path, queryString, null);
        }

        public async Task<RestResponse> ModifyOrderAsync(string qty, string price, string orderId = null, string clientId = null, string tpPrice = null, string tpStopType = null, string tpOrderType = null, string tpOrderPrice = null, string slPrice = null, string slStopType = null, string slOrderType = null, string slOrderPrice = null)
        {
            if (string.IsNullOrWhiteSpace(orderId) && string.IsNullOrWhiteSpace(clientId))
                throw new ArgumentException("Either orderId or clientId must be provided.");

            var path = "/api/v1/futures/trade/modify_order";

            var bodyObj = new Dictionary<string, object>
            {
                { "qty", qty },
                { "price", price }
            };

            if (!string.IsNullOrWhiteSpace(orderId)) bodyObj["orderId"] = orderId;
            else if (!string.IsNullOrWhiteSpace(clientId)) bodyObj["clientId"] = clientId;

            if (!string.IsNullOrWhiteSpace(tpPrice)) bodyObj["tpPrice"] = tpPrice;
            if (!string.IsNullOrWhiteSpace(tpStopType)) bodyObj["tpStopType"] = tpStopType;
            if (!string.IsNullOrWhiteSpace(tpOrderType)) bodyObj["tpOrderType"] = tpOrderType;
            if (!string.IsNullOrWhiteSpace(tpOrderPrice)) bodyObj["tpOrderPrice"] = tpOrderPrice;

            if (!string.IsNullOrWhiteSpace(slPrice)) bodyObj["slPrice"] = slPrice;
            if (!string.IsNullOrWhiteSpace(slStopType)) bodyObj["slStopType"] = slStopType;
            if (!string.IsNullOrWhiteSpace(slOrderType)) bodyObj["slOrderType"] = slOrderType;
            if (!string.IsNullOrWhiteSpace(slOrderPrice)) bodyObj["slOrderPrice"] = slOrderPrice;

            string bodyJson = JsonConvert.SerializeObject(bodyObj, Formatting.None);

            return await SendRequestAsync("POST", path, "", bodyJson);
        }

        public async Task<RestResponse> PlaceOrderAsync(string symbol, string qty, string side, string tradeSide, string orderType, string price = null, string effect = null, string clientId = null, string positionId = null, bool? reduceOnly = null, string tpPrice = null, string tpStopType = null, string tpOrderType = null, string tpOrderPrice = null, string slPrice = null, string slStopType = null, string slOrderType = null, string slOrderPrice = null)
        {
            var path = "/api/v1/futures/trade/place_order";

            var bodyObj = new Dictionary<string, object>
            {
                { "symbol", symbol },
                { "qty", qty },
                { "side", side },
                { "tradeSide", tradeSide },
                { "orderType", orderType }
            };

            if (!string.IsNullOrWhiteSpace(price)) bodyObj["price"] = price;
            if (!string.IsNullOrWhiteSpace(effect)) bodyObj["effect"] = effect;
            if (!string.IsNullOrWhiteSpace(clientId)) bodyObj["clientId"] = clientId;
            if (!string.IsNullOrWhiteSpace(positionId)) bodyObj["positionId"] = positionId;
            if (reduceOnly.HasValue) bodyObj["reduceOnly"] = reduceOnly.Value;

            if (!string.IsNullOrWhiteSpace(tpPrice)) bodyObj["tpPrice"] = tpPrice;
            if (!string.IsNullOrWhiteSpace(tpStopType)) bodyObj["tpStopType"] = tpStopType;
            if (!string.IsNullOrWhiteSpace(tpOrderType)) bodyObj["tpOrderType"] = tpOrderType;
            if (!string.IsNullOrWhiteSpace(tpOrderPrice)) bodyObj["tpOrderPrice"] = tpOrderPrice;

            if (!string.IsNullOrWhiteSpace(slPrice)) bodyObj["slPrice"] = slPrice;
            if (!string.IsNullOrWhiteSpace(slStopType)) bodyObj["slStopType"] = slStopType;
            if (!string.IsNullOrWhiteSpace(slOrderType)) bodyObj["slOrderType"] = slOrderType;
            if (!string.IsNullOrWhiteSpace(slOrderPrice)) bodyObj["slOrderPrice"] = slOrderPrice;

            string bodyJson = JsonConvert.SerializeObject(bodyObj, Formatting.None);

            return await SendRequestAsync("POST", path, "", bodyJson);
        }


        #endregion

        #region Take Profit/ Stop Loss (Tp/Sl)

        public async Task<RestResponse> CancelTpSlOrderAsync(string symbol, string orderId)
        {
            var path = "/api/v1/futures/tpsl/cancel_order";

            var bodyObj = new
            {
                symbol = symbol,
                orderId = orderId
            };

            string bodyJson = JsonConvert.SerializeObject(bodyObj, Formatting.None);

            return await SendRequestAsync("POST", path, "", bodyJson);
        }

        public async Task<RestResponse> GetTpSlOrderHistoryAsync(string symbol = null, int? side = null, int? positionMode = null, long? startTime = null, long? endTime = null, long? skip = null, long? limit = null)
        {
            var path = "/api/v1/futures/tpsl/get_history_orders";

            var queryParams = new List<string>();

            if (!string.IsNullOrWhiteSpace(symbol)) queryParams.Add($"symbol={symbol}");
            if (side.HasValue) queryParams.Add($"side={side}");
            if (positionMode.HasValue) queryParams.Add($"positionMode={positionMode}");
            if (startTime.HasValue) queryParams.Add($"startTime={startTime}");
            if (endTime.HasValue) queryParams.Add($"endTime={endTime}");
            if (skip.HasValue) queryParams.Add($"skip={skip}");
            if (limit.HasValue) queryParams.Add($"limit={limit}");

            string queryString = string.Join("&", queryParams);

            return await SendRequestAsync("GET", path, queryString, null);
        }

        public async Task<RestResponse> GetPendingTpSlOrdersAsync(string symbol = null, string positionId = null, int? side = null, int? positionMode = null, long? skip = null, long? limit = null)
        {
            var path = "/api/v1/futures/tpsl/get_pending_orders";

            var queryParams = new List<string>();

            if (!string.IsNullOrWhiteSpace(symbol)) queryParams.Add($"symbol={symbol}");
            if (!string.IsNullOrWhiteSpace(positionId)) queryParams.Add($"positionId={positionId}");
            if (side.HasValue) queryParams.Add($"side={side}");
            if (positionMode.HasValue) queryParams.Add($"positionMode={positionMode}");
            if (skip.HasValue) queryParams.Add($"skip={skip}");
            if (limit.HasValue) queryParams.Add($"limit={limit}");

            string queryString = string.Join("&", queryParams);

            return await SendRequestAsync("GET", path, queryString, null);
        }

        public async Task<RestResponse> ModifyPositionTpSlOrderAsync(string symbol, string positionId, string tpPrice = null, string tpStopType = null, string slPrice = null, string slStopType = null)
        {
            if (string.IsNullOrWhiteSpace(tpPrice) && string.IsNullOrWhiteSpace(slPrice))
                throw new ArgumentException("At least one of tpPrice or slPrice must be provided.");

            var path = "/api/v1/futures/tpsl/position/modify_order";

            var bodyObj = new Dictionary<string, object>
            {
                { "symbol", symbol },
                { "positionId", positionId }
            };

            if (!string.IsNullOrWhiteSpace(tpPrice)) bodyObj["tpPrice"] = tpPrice;
            if (!string.IsNullOrWhiteSpace(tpStopType)) bodyObj["tpStopType"] = tpStopType;
            if (!string.IsNullOrWhiteSpace(slPrice)) bodyObj["slPrice"] = slPrice;
            if (!string.IsNullOrWhiteSpace(slStopType)) bodyObj["slStopType"] = slStopType;

            string bodyJson = JsonConvert.SerializeObject(bodyObj, Formatting.None);

            return await SendRequestAsync("POST", path, "", bodyJson);
        }

        public async Task<RestResponse> ModifyTpSlOrderAsync(string orderId, string tpPrice = null, string tpStopType = null, string tpOrderType = null, string tpOrderPrice = null, string tpQty = null, string slPrice = null, string slStopType = null, string slOrderType = null, string slOrderPrice = null, string slQty = null)
        {
            if (string.IsNullOrWhiteSpace(tpPrice) && string.IsNullOrWhiteSpace(slPrice))
                throw new ArgumentException("At least one of tpPrice or slPrice must be provided.");

            if (string.IsNullOrWhiteSpace(tpQty) && string.IsNullOrWhiteSpace(slQty))
                throw new ArgumentException("At least one of tpQty or slQty must be provided.");

            var path = "/api/v1/futures/tpsl/modify_order";

            var bodyObj = new Dictionary<string, object>
            {
                { "orderId", orderId }
            };

            if (!string.IsNullOrWhiteSpace(tpPrice)) bodyObj["tpPrice"] = tpPrice;
            if (!string.IsNullOrWhiteSpace(tpStopType)) bodyObj["tpStopType"] = tpStopType;
            if (!string.IsNullOrWhiteSpace(tpOrderType)) bodyObj["tpOrderType"] = tpOrderType;
            if (!string.IsNullOrWhiteSpace(tpOrderPrice)) bodyObj["tpOrderPrice"] = tpOrderPrice;
            if (!string.IsNullOrWhiteSpace(tpQty)) bodyObj["tpQty"] = tpQty;

            if (!string.IsNullOrWhiteSpace(slPrice)) bodyObj["slPrice"] = slPrice;
            if (!string.IsNullOrWhiteSpace(slStopType)) bodyObj["slStopType"] = slStopType;
            if (!string.IsNullOrWhiteSpace(slOrderType)) bodyObj["slOrderType"] = slOrderType;
            if (!string.IsNullOrWhiteSpace(slOrderPrice)) bodyObj["slOrderPrice"] = slOrderPrice;
            if (!string.IsNullOrWhiteSpace(slQty)) bodyObj["slQty"] = slQty;

            string bodyJson = JsonConvert.SerializeObject(bodyObj, Formatting.None);

            return await SendRequestAsync("POST", path, "", bodyJson);
        }

        public async Task<RestResponse> PlacePositionTpSlOrderAsync(string symbol, string positionId, string tpPrice = null, string tpStopType = null, string slPrice = null, string slStopType = null)
        {
            if (string.IsNullOrWhiteSpace(tpPrice) && string.IsNullOrWhiteSpace(slPrice))
                throw new ArgumentException("At least one of tpPrice or slPrice must be provided.");

            var path = "/api/v1/futures/tpsl/position/place_order";

            var bodyObj = new Dictionary<string, object>
            {
                { "symbol", symbol },
                { "positionId", positionId }
            };

            if (!string.IsNullOrWhiteSpace(tpPrice)) bodyObj["tpPrice"] = tpPrice;
            if (!string.IsNullOrWhiteSpace(tpStopType)) bodyObj["tpStopType"] = tpStopType;
            if (!string.IsNullOrWhiteSpace(slPrice)) bodyObj["slPrice"] = slPrice;
            if (!string.IsNullOrWhiteSpace(slStopType)) bodyObj["slStopType"] = slStopType;

            string bodyJson = JsonConvert.SerializeObject(bodyObj, Formatting.None);

            return await SendRequestAsync("POST", path, "", bodyJson);
        }

        public async Task<RestResponse> PlaceTpSlOrderAsync(string symbol, string positionId, string tpPrice = null, string tpStopType = null, string tpOrderType = null, string tpOrderPrice = null, string tpQty = null, string slPrice = null, string slStopType = null, string slOrderType = null, string slOrderPrice = null, string slQty = null)
        {
            if (string.IsNullOrWhiteSpace(tpPrice) && string.IsNullOrWhiteSpace(slPrice))
                throw new ArgumentException("At least one of tpPrice or slPrice must be provided.");

            if (string.IsNullOrWhiteSpace(tpQty) && string.IsNullOrWhiteSpace(slQty))
                throw new ArgumentException("At least one of tpQty or slQty must be provided.");

            var path = "/api/v1/futures/tpsl/place_order";

            var bodyObj = new Dictionary<string, object>
            {
                { "symbol", symbol },
                { "positionId", positionId }
            };

            if (!string.IsNullOrWhiteSpace(tpPrice)) bodyObj["tpPrice"] = tpPrice;
            if (!string.IsNullOrWhiteSpace(tpStopType)) bodyObj["tpStopType"] = tpStopType;
            if (!string.IsNullOrWhiteSpace(tpOrderType)) bodyObj["tpOrderType"] = tpOrderType;
            if (!string.IsNullOrWhiteSpace(tpOrderPrice)) bodyObj["tpOrderPrice"] = tpOrderPrice;
            if (!string.IsNullOrWhiteSpace(tpQty)) bodyObj["tpQty"] = tpQty;

            if (!string.IsNullOrWhiteSpace(slPrice)) bodyObj["slPrice"] = slPrice;
            if (!string.IsNullOrWhiteSpace(slStopType)) bodyObj["slStopType"] = slStopType;
            if (!string.IsNullOrWhiteSpace(slOrderType)) bodyObj["slOrderType"] = slOrderType;
            if (!string.IsNullOrWhiteSpace(slOrderPrice)) bodyObj["slOrderPrice"] = slOrderPrice;
            if (!string.IsNullOrWhiteSpace(slQty)) bodyObj["slQty"] = slQty;

            string bodyJson = JsonConvert.SerializeObject(bodyObj, Formatting.None);

            return await SendRequestAsync("POST", path, "", bodyJson);
        }


        #endregion

        #region Market  

        public async Task<RestResponse> GetMarketDepthAsync(string symbol, string limit = null)
        {
            if (string.IsNullOrWhiteSpace(symbol))
                throw new ArgumentException("symbol is required.");

            var path = "/api/v1/futures/market/depth";

            var queryParams = new List<string> { $"symbol={symbol}" };

            if (!string.IsNullOrWhiteSpace(limit))
                queryParams.Add($"limit={limit}");

            string queryString = string.Join("&", queryParams);

            return await SendRequestAsync("GET", path, queryString, null);
        }

        public async Task<RestResponse> GetFundingRateAsync(string symbol)
        {
            if (string.IsNullOrWhiteSpace(symbol))
                throw new ArgumentException("symbol is required.");

            var path = "/api/v1/futures/market/funding_rate";
            var queryParams = $"symbol={symbol}";

            return await SendRequestAsync("GET", path, queryParams, null);
        }

        public async Task<RestResponse> GetKlineAsync(string symbol, string interval, long? startTime = null, long? endTime = null, int? limit = null, string type = null)
        {
            if (string.IsNullOrWhiteSpace(symbol))
                throw new ArgumentException("symbol is required.");
            if (string.IsNullOrWhiteSpace(interval))
                throw new ArgumentException("interval is required.");

            var path = "/api/v1/futures/market/kline";

            var queryParams = new List<string>
            {
                $"symbol={symbol}",
                $"interval={interval}"
            };

            if (startTime.HasValue) queryParams.Add($"startTime={startTime}");
            if (endTime.HasValue) queryParams.Add($"endTime={endTime}");
            if (limit.HasValue) queryParams.Add($"limit={limit}");
            if (!string.IsNullOrWhiteSpace(type)) queryParams.Add($"type={type}");

            string queryString = string.Join("&", queryParams);

            return await SendRequestAsync("GET", path, queryString, null);
        }

        public async Task<RestResponse> GetTickersAsync(string symbols = null)
        {
            var path = "/api/v1/futures/market/tickers";

            string queryString = string.IsNullOrWhiteSpace(symbols)
                ? ""
                : $"symbols={symbols}";

            return await SendRequestAsync("GET", path, queryString, null);
        }

        public async Task<RestResponse> GetTradingPairsAsync(string symbols = null)
        {
            var path = "/api/v1/futures/market/trading_pairs";

            string queryString = string.IsNullOrWhiteSpace(symbols)
                ? ""
                : $"symbols={symbols}";

            return await SendRequestAsync("GET", path, queryString, null);
        }

        #endregion

        #region Position

        public async Task<RestResponse> GetHistoryPositionsAsync(string symbol = null, string positionId = null, long? startTime = null, long? endTime = null, long? skip = null, long? limit = null)
        {
            var path = "/api/v1/futures/position/get_history_positions";

            var queryParams = new List<string>();

            if (!string.IsNullOrWhiteSpace(symbol)) queryParams.Add($"symbol={symbol}");
            if (!string.IsNullOrWhiteSpace(positionId)) queryParams.Add($"positionId={positionId}");
            if (startTime.HasValue) queryParams.Add($"startTime={startTime}");
            if (endTime.HasValue) queryParams.Add($"endTime={endTime}");
            if (skip.HasValue) queryParams.Add($"skip={skip}");
            if (limit.HasValue) queryParams.Add($"limit={limit}");

            string queryString = string.Join("&", queryParams);

            return await SendRequestAsync("GET", path, queryString, null);
        }

        public async Task<RestResponse> GetPendingPositionsAsync(string symbol = null, string positionId = null)
        {
            var path = "/api/v1/futures/position/get_pending_positions";

            var queryParams = new List<string>();

            if (!string.IsNullOrWhiteSpace(symbol)) queryParams.Add($"symbol={symbol}");
            if (!string.IsNullOrWhiteSpace(positionId)) queryParams.Add($"positionId={positionId}");

            string queryString = string.Join("&", queryParams);

            return await SendRequestAsync("GET", path, queryString, null);
        }

        public async Task<RestResponse> GetPositionTiersAsync(string symbol)
        {
            if (string.IsNullOrWhiteSpace(symbol))
                throw new ArgumentException("symbol is required.");

            var path = "/api/v1/futures/position/get_position_tiers";
            var queryString = $"symbol={symbol}";

            return await SendRequestAsync("GET", path, queryString, null);
        }


        #endregion

    }
}