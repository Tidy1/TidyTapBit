using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using RestSharp;

using System.Net.Http;
using System.Security.Cryptography;
using System.Text;

using TidyTrader.ApiIntegration.Interfaces;
using TidyTrader.ApiIntegration.Models.Responses.Account;
using TidyTrader.ApiIntegration.Models.Responses.Market;
using TidyTrader.ApiIntegration.Models.Responses.Position;
using TidyTrader.ApiIntegration.Models.Responses.TpSl;
using TidyTrader.ApiIntegration.Models.Responses.Trade;

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

        private async Task<ApiResponse<T>> SendRequestAsync<T>(string method, string path, string queryParams = "", object body = null)
        {
            var client = new RestClient(_baseUrl);
            var fullPath = path + (string.IsNullOrWhiteSpace(queryParams) ? "" : $"?{queryParams}");
            var request = new RestRequest(fullPath, method == "GET" ? Method.Get : Method.Post);

            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString(); // as string
            string nonce = Guid.NewGuid().ToString("N").Substring(0, 32);

            string compactBody = (body != null && method != "GET")
                ? JsonConvert.SerializeObject(body, Formatting.None)
                : "";

            string sortedQueryParams = FlattenQueryParams(queryParams);

            // ❌ Do NOT trim this line!
            string digestInput = $"{nonce}{timestamp}{_apiKey}{sortedQueryParams}{compactBody}";

            // Apply first SHA256
            string digest = Sha256Hex(digestInput);

            // Apply second SHA256 with secret
            string signature = Sha256Hex(digest + _apiSecret);


            request.AddOrUpdateHeader("api-key", _apiKey);
            request.AddOrUpdateHeader("sign", signature);
            request.AddOrUpdateHeader("nonce", nonce);
            request.AddOrUpdateHeader("timestamp", timestamp);
            //request.AddOrUpdateHeader("language", "en-US");
            request.AddOrUpdateHeader("Content-Type", "application/json");

            if (method != "GET" && !string.IsNullOrEmpty(compactBody))
            {
                request.AddStringBody(compactBody, DataFormat.Json);
            }

            try
            {
                var response = await client.ExecuteAsync(request);

                var result = new ApiResponse<T>
                {
                    StatusCode = response.StatusCode,
                    ErrorMessage = response.ErrorMessage,
                    ErrorException = response.ErrorException
                };

                if (response.IsSuccessful)
                {
                    result.Data = JsonConvert.DeserializeObject<T>(response.Content);
                }

                return result;
            }
            catch (Exception ex)
            {
                return new ApiResponse<T>
                {
                    StatusCode = 0,
                    ErrorMessage = "Exception occurred during request.",
                    ErrorException = ex
                };
            }
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
                    return parts[0] + (parts.Length > 1 ? parts[1] : "");
                })
                .OrderBy(kv => kv, StringComparer.Ordinal)
            );
        }


        private static string Sha256Hex(string input)
        {
            using var sha256 = SHA256.Create();
            var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
            return BitConverter.ToString(hash).Replace("-", "").ToLower();
        }


        #region Account
        public async Task<decimal> GetUsdtAvailableAsync()
        {
            var resp = await GetSingleAccountAsync("USDT");

            // Check for success and non-null Data
            if (resp != null && resp.IsSuccessStatusCode && resp.Data != null && resp.Data.Data != null)
            {
                decimal.TryParse(resp.Data.Data.Available, out decimal availableBalance);
                return availableBalance;
            }

            // Optionally log error details for debugging
            if (resp != null && !resp.IsSuccessStatusCode)
            {
                // Log resp.ErrorMessage or resp.ErrorException as needed
            }

            return 0m;
        }


        public async Task<ApiResponse<AdjustPositionMarginResponse>> AdjustPositionMarginAsync(string marginCoin, string symbol, decimal amount, string side = null, string positionId = null)
        {
            var path = "/api/v1/futures/account/adjust_position_margin";

            var bodyObj = new Dictionary<string, object>
            {
                { "symbol", symbol },
                { "marginCoin", marginCoin },
                { "amount", amount.ToString("0.########") }
            };

            if (!string.IsNullOrWhiteSpace(side)) bodyObj["side"] = side;
            if (!string.IsNullOrWhiteSpace(positionId)) bodyObj["positionId"] = positionId;

            return await SendRequestAsync<AdjustPositionMarginResponse>("POST", path, "", bodyObj);
        }

        public async Task<ApiResponse<ChangeLeverageResponse>> ChangeLeverageAsync(string symbol, string marginCoin, int leverage)
        {
            var path = "/api/v1/futures/account/set_leverage";

            var bodyObj = new
            {
                symbol = symbol,
                marginCoin = marginCoin,
                leverage = leverage
            };

            string bodyJson = JsonConvert.SerializeObject(bodyObj, Formatting.None);

            return await SendRequestAsync<ChangeLeverageResponse>("POST", path, "", bodyJson);
        }

        public async Task<ApiResponse<ChangeMarginModeResponse>> ChangeMarginModeAsync(string symbol, string marginCoin, string marginMode)
        {
            var path = "/api/v1/futures/account/set_margin_mode";

            var bodyObj = new
            {
                symbol = symbol,
                marginCoin = marginCoin,
                marginMode = marginMode
            };

            string bodyJson = JsonConvert.SerializeObject(bodyObj, Formatting.None);

            return await SendRequestAsync<ChangeMarginModeResponse>("POST", path, "", bodyJson);
        }

        /// <summary>
        /// await apiClient.ChangePositionModeAsync("BTCUSDT", "USDT", 1); // One-way mode
        //  await apiClient.ChangePositionModeAsync("BTCUSDT", "USDT", 2); // Hedge mode
        /// </summary>
        /// <param name="symbol"></param>
        /// <param name="marginCoin"></param>
        /// <param name="positionMode"></param>
        /// <returns></returns>
        public async Task<ApiResponse<ChangePositionModeResponse>> ChangePositionModeAsync(string symbol, string marginCoin, int positionMode)
        {
            var path = "/api/v1/futures/account/set_position_mode";

            var bodyObj = new
            {
                symbol = symbol,
                marginCoin = marginCoin,
                positionMode = positionMode
            };

            string bodyJson = JsonConvert.SerializeObject(bodyObj, Formatting.None);

            return await SendRequestAsync<ChangePositionModeResponse>("POST", path, "", bodyJson);
        }

        public async Task<ApiResponse<GetLeverageMarginModeResponse>> GetLeverageAndMarginModeAsync(string symbol, string marginCoin)
        {
            var path = "/api/v1/futures/account/get_leverage_and_margin_mode";
            var queryParams = $"symbol={symbol}&marginCoin={marginCoin}";

            return await SendRequestAsync<GetLeverageMarginModeResponse>("GET", path, queryParams, null);
        }

        public async Task<ApiResponse<GetSingleAccountResponse>> GetSingleAccountAsync(string marginCoin)
        {
            var queryParams = $"marginCoin={marginCoin}";
            var path = "/api/v1/futures/account";

            return await SendRequestAsync<GetSingleAccountResponse>("GET", path, queryParams, null);
        }

        #endregion

        #region Trade

      

        public async Task<ApiResponse<BatchOrderResponse>> BatchOrdersAsync(string symbol, string marginCoin, IEnumerable<object> orderDataList)
        {
            var path = "/api/v1/futures/trade/batch_order";

            var bodyObj = new
            {
                symbol = symbol,
                marginCoin = marginCoin,
                orderDataList = orderDataList
            };

            string bodyJson = JsonConvert.SerializeObject(bodyObj, Formatting.None);

            return await SendRequestAsync<BatchOrderResponse>("POST", path, "", bodyJson);
        }

        public async Task<ApiResponse<CancelAllOrdersResponse>> CancelAllOrdersAsync(string symbol)
        {
            var path = "/api/v1/futures/trade/cancel_all_orders";

            // Prepare body JSON, Bitunix requires POST with body = { "symbol": "BTCUSDT" }
            var bodyObj = new { symbol = symbol };
            string bodyJson = JsonConvert.SerializeObject(bodyObj, Formatting.None);

            // No query params for this endpoint
            return await SendRequestAsync<CancelAllOrdersResponse>("POST", path, "", bodyJson);
        }

        public async Task<ApiResponse<CancelOrdersResponse>> CancelOrdersAsync(string symbol, IEnumerable<string> orderIds)
        {
            var path = "/api/v1/futures/trade/cancel_orders";

            var bodyObj = new
            {
                symbol = symbol,
                orderIds = orderIds
            };

            string bodyJson = JsonConvert.SerializeObject(bodyObj, Formatting.None);

            return await SendRequestAsync<CancelOrdersResponse>("POST", path, "", bodyJson);
        }

        public async Task<ApiResponse<CloseAllPositionResponse>> CloseAllPositionsAsync(string symbol)
        {
            var path = "/api/v1/futures/trade/close_all_position";

            var bodyObj = new
            {
                symbol = symbol
            };

            string bodyJson = JsonConvert.SerializeObject(bodyObj, Formatting.None);

            return await SendRequestAsync<CloseAllPositionResponse>("POST", path, "", bodyJson);
        }

        public async Task<ApiResponse<FlashClosePositionResponse>> FlashClosePositionAsync(string positionId)
        {
            var path = "/api/v1/futures/trade/flash_close_position";

            var bodyObj = new
            {
                positionId = positionId
            };

            string bodyJson = JsonConvert.SerializeObject(bodyObj, Formatting.None);

            return await SendRequestAsync<FlashClosePositionResponse>("POST", path, "", bodyJson);
        }

        public async Task<ApiResponse<GetOrderHistoryResponse>> GetHistoryOrdersAsync(string symbol = null, string orderId = null, string clientId = null, string status = null, string type = null, long? startTime = null, long? endTime = null, long? skip = null, long? limit = null)
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

            return await SendRequestAsync<GetOrderHistoryResponse>("GET", path, queryString, null);
        }

        public async Task<ApiResponse<GetTradeHistoryResponse>> GetHistoryTradesAsync(string symbol = null, string orderId = null, string positionId = null, long? startTime = null, long? endTime = null, long? skip = null, long? limit = null)
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

            return await SendRequestAsync<GetTradeHistoryResponse>("GET", path, queryString, null);
        }

        public async Task<ApiResponse<GetOrderDetailResponse>> GetOrderDetailAsync(string orderId = null, string clientId = null)
        {
            if (string.IsNullOrWhiteSpace(orderId) && string.IsNullOrWhiteSpace(clientId))
                throw new ArgumentException("Either orderId or clientId must be provided.");

            var path = "/api/v1/futures/trade/get_order_detail";

            var queryParams = new List<string>();
            if (!string.IsNullOrWhiteSpace(orderId)) queryParams.Add($"orderId={orderId}");
            if (!string.IsNullOrWhiteSpace(clientId)) queryParams.Add($"clientId={clientId}");

            string queryString = string.Join("&", queryParams);

            return await SendRequestAsync<GetOrderDetailResponse>("GET", path, queryString, null);
        }

        public async Task<ApiResponse<GetPendingOrdersResponse>> GetPendingOrdersAsync(string symbol = null, string orderId = null, string clientId = null, string status = null, long? startTime = null, long? endTime = null, long? skip = null, long? limit = null)
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

            return await SendRequestAsync<GetPendingOrdersResponse>("GET", path, queryString, null);
        }

        public async Task<ApiResponse<ModifyOrderResponse>> ModifyOrderAsync(string qty, string price, string orderId = null, string clientId = null, string tpPrice = null, string tpStopType = null, string tpOrderType = null, string tpOrderPrice = null, string slPrice = null, string slStopType = null, string slOrderType = null, string slOrderPrice = null)
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

            return await SendRequestAsync<ModifyOrderResponse>("POST", path, "", bodyJson);
        }

        public async Task<ApiResponse<PlaceOrderResponse>> PlaceOrderAsync(string symbol, string qty, string side, string tradeSide, string orderType, string price = null, string effect = null, string clientId = null, string positionId = null, bool? reduceOnly = null, string tpPrice = null, string tpStopType = null, string tpOrderType = null, string tpOrderPrice = null, string slPrice = null, string slStopType = null, string slOrderType = null, string slOrderPrice = null)
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

            // ✅ Pass the object, not the serialized string
            return await SendRequestAsync<PlaceOrderResponse>("POST", path, "", bodyObj);
        }


        #endregion

        #region Take Profit/ Stop Loss (Tp/Sl)

        public async Task<ApiResponse<CancelTpSlOrderResponse>> CancelTpSlOrderAsync(string symbol, string orderId)
        {
            var path = "/api/v1/futures/tpsl/cancel_order";

            var bodyObj = new
            {
                symbol = symbol,
                orderId = orderId
            };

            string bodyJson = JsonConvert.SerializeObject(bodyObj, Formatting.None);

            return await SendRequestAsync<CancelTpSlOrderResponse>("POST", path, "", bodyJson);
        }

        public async Task<ApiResponse<GetHistoryTpSlOrdersResponse>> GetTpSlOrderHistoryAsync(string symbol = null, int? side = null, int? positionMode = null, long? startTime = null, long? endTime = null, long? skip = null, long? limit = null)
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

            return await SendRequestAsync<GetHistoryTpSlOrdersResponse>("GET", path, queryString, null);
        }

        public async Task<ApiResponse<GetPendingTpSlOrdersResponse>> GetPendingTpSlOrdersAsync(string symbol = null, string positionId = null, int? side = null, int? positionMode = null, long? skip = null, long? limit = null)
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

            return await SendRequestAsync<GetPendingTpSlOrdersResponse>("GET", path, queryString, null);
        }

        public async Task<ApiResponse<ModifyPositionTpSlOrderResponse>> ModifyPositionTpSlOrderAsync(string symbol, string positionId, string tpPrice = null, string tpStopType = null, string slPrice = null, string slStopType = null)
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

            return await SendRequestAsync<ModifyPositionTpSlOrderResponse>("POST", path, "", bodyJson);
        }

        public async Task<ApiResponse<ModifyTpSlOrderResponse>> ModifyTpSlOrderAsync(string orderId, string tpPrice = null, string tpStopType = null, string tpOrderType = null, string tpOrderPrice = null, string tpQty = null, string slPrice = null, string slStopType = null, string slOrderType = null, string slOrderPrice = null, string slQty = null)
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

            return await SendRequestAsync<ModifyTpSlOrderResponse>("POST", path, "", bodyJson);
        }

        public async Task<ApiResponse<PlacePositionTpSlOrderResponse>> PlacePositionTpSlOrderAsync(string symbol, string positionId, string tpPrice = null, string tpStopType = null, string slPrice = null, string slStopType = null)
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

            return await SendRequestAsync<PlacePositionTpSlOrderResponse>("POST", path, "", bodyJson);
        }

        public async Task<ApiResponse<PlaceTpSlOrderResponse>> PlaceTpSlOrderAsync(string symbol, string positionId, string tpPrice = null, string tpStopType = null, string tpOrderType = null, string tpOrderPrice = null, string tpQty = null, string slPrice = null, string slStopType = null, string slOrderType = null, string slOrderPrice = null, string slQty = null)
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

            return await SendRequestAsync<PlaceTpSlOrderResponse>("POST", path, "", bodyJson);
        }


        #endregion

        #region Market  

        /// <summary>
        /// Get market depth data for a specific symbol.
        /// </summary>
        /// <param name="symbol"></param>
        /// <param name="limit"></param>
        /// <returns></returns>
        /// <remarks>Fixed gear enumeration value: 1/5/15/50/max,</remarks>
        /// <exception cref="ArgumentException"></exception>
        public async Task<ApiResponse<MarketDepthResponse>> GetMarketDepthAsync(string symbol, string limit = null)
        {
            if (string.IsNullOrWhiteSpace(symbol))
                throw new ArgumentException("symbol is required.");

            var path = "/api/v1/futures/market/depth";

            var queryParams = new List<string> { $"symbol={symbol}" };

            if (!string.IsNullOrWhiteSpace(limit))
                queryParams.Add($"limit={limit}");

            string queryString = string.Join("&", queryParams);

            return await SendRequestAsync<MarketDepthResponse>("GET", path, queryString, null);
        }

        public async Task<ApiResponse<FundingRateResponse>> GetFundingRateAsync(string symbol)
        {
            if (string.IsNullOrWhiteSpace(symbol))
                throw new ArgumentException("symbol is required.");

            var path = "/api/v1/futures/market/funding_rate";
            var queryParams = $"symbol={symbol}";

            return await SendRequestAsync<FundingRateResponse>("GET", path, queryParams, null);
        }

        public async Task<ApiResponse<KlineResponse>> GetKlineAsync(string symbol, string interval, long? startTime = null, long? endTime = null, int? limit = null, string type = null)
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

            return await SendRequestAsync<KlineResponse>("GET", path, queryString, null);
        }

        public async Task<ApiResponse<TickerResponse>> GetTickersAsync(string symbols = null)
        {
            var path = "/api/v1/futures/market/tickers";

            string queryString = string.IsNullOrWhiteSpace(symbols)
                ? ""
                : $"symbols={symbols}";

            return await SendRequestAsync<TickerResponse>("GET", path, queryString, null);
        }

        public async Task<ApiResponse<TradingPairsResponse>> GetTradingPairsAsync(string symbols = null)
        {
            var path = "/api/v1/futures/market/trading_pairs";

            string queryString = string.IsNullOrWhiteSpace(symbols)
                ? ""
                : $"symbols={symbols}";

            return await SendRequestAsync<TradingPairsResponse>("GET", path, queryString, null);
        }

        #endregion

        #region Position

        public async Task<ApiResponse<HistoryPositionsResponse>> GetHistoryPositionsAsync(string symbol = null, string positionId = null, long? startTime = null, long? endTime = null, long? skip = null, long? limit = null)
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

            return await SendRequestAsync<HistoryPositionsResponse>("GET", path, queryString, null);
        }

        public async Task<ApiResponse<PendingPositionsResponse>> GetPendingPositionsAsync(string symbol = null, string positionId = null)
        {
            var path = "/api/v1/futures/position/get_pending_positions";

            var queryParams = new List<string>();

            if (!string.IsNullOrWhiteSpace(symbol)) queryParams.Add($"symbol={symbol}");
            if (!string.IsNullOrWhiteSpace(positionId)) queryParams.Add($"positionId={positionId}");

            string queryString = string.Join("&", queryParams);

            return await SendRequestAsync<PendingPositionsResponse>("GET", path, queryString, null);
        }

        public async Task<ApiResponse<PositionTiersResponse>> GetPositionTiersAsync(string symbol)
        {
            if (string.IsNullOrWhiteSpace(symbol))
                throw new ArgumentException("symbol is required.");

            var path = "/api/v1/futures/position/get_position_tiers";
            var queryString = $"symbol={symbol}";

            return await SendRequestAsync<PositionTiersResponse>("GET", path, queryString, null);
        }


        #endregion

    }
}