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

        public async Task<RestResponse> GetAccountInfoAsync(string marginCoin)
        {
            var queryParams = $"marginCoin={marginCoin}";
            var path = "/api/v1/futures/account";

            return await SendRequestAsync("GET", path, queryParams, null);
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

            // Log for debug
            Console.WriteLine("=== Bitunix Signature Debug ===");
            Console.WriteLine($"Method: {method}");
            Console.WriteLine($"Path: {path}");
            Console.WriteLine($"QueryParams: {queryParams}");
            Console.WriteLine($"SortedParams: {sortedQueryParams}");
            Console.WriteLine($"Digest Input: {digestInput}");
            Console.WriteLine($"Digest: {digest}");
            Console.WriteLine($"Signature Input: {digest + _apiSecret}");
            Console.WriteLine($"Signature: {signature}");
            Console.WriteLine($"Full Path: {fullPath}");
            Console.WriteLine($"Request Body: {(method == "GET" ? "(not sent)" : compactBody)}");
            Console.WriteLine("================================");

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

        public Task<string> CancelAllOrdersAsync(string symbol)
        {
            throw new NotImplementedException();
        }

        public Task<string> CancelOrdersAsync(string symbol, string[] orderIds)
        {
            throw new NotImplementedException();
        }

        public Task<string> CancelTpSlOrderAsync(string symbol, string orderId)
        {
            throw new NotImplementedException();
        }

        public Task<string> GetFundingRateAsync(string symbol)
        {
            throw new NotImplementedException();
        }

        public Task<string> GetHistoryPositionsAsync(string symbol, int limit)
        {
            throw new NotImplementedException();
        }

        public Task<string> GetHistoryTpSlOrderAsync(string symbol)
        {
            throw new NotImplementedException();
        }

        public Task<string> GetHistoryTradesAsync(string? symbol = null, long? startTime = null, long? endTime = null, int? limit = null)
        {
            throw new NotImplementedException();
        }

        public Task<string> GetKlineAsync(string symbol, string interval, int limit)
        {
            throw new NotImplementedException();
        }

        public Task<string> GetMarketDepthAsync(string symbol, string limit)
        {
            throw new NotImplementedException();
        }

        public Task<string> GetMarketTickersAsync(string? symbol = null)
        {
            throw new NotImplementedException();
        }

        public Task<string> GetPendingPositionsAsync(string symbol)
        {
            throw new NotImplementedException();
        }

        public Task<string> GetPendingTpSlOrderAsync(string symbol)
        {
            throw new NotImplementedException();
        }

        public Task<string> GetPositionTiersAsync(string symbol)
        {
            throw new NotImplementedException();
        }

        public Task<string> GetServerTimeAsync()
        {
            throw new NotImplementedException();
        }

        public Task<string> GetTradingPairsAsync()
        {
            throw new NotImplementedException();
        }

        public Task<string> ModifyPositionTpSlOrderAsync(string symbol, string orderId, decimal stopLoss, decimal takeProfit)
        {
            throw new NotImplementedException();
        }

        public Task<string> ModifyTpSlOrderAsync(string symbol, string orderId, decimal stopLoss, decimal takeProfit)
        {
            throw new NotImplementedException();
        }

        public Task<string> PlaceFuturesOrderAsync(string symbol, decimal qty, string side, string orderType, int leverage, decimal? price = null, string clientOrderId = null, string timeInForce = null, bool? reduceOnly = null, bool? postOnly = null, string positionSide = null)
        {
            throw new NotImplementedException();
        }

        public Task<string> PlacePositionTpSlOrderAsync(string symbol, decimal stopLoss, decimal takeProfit)
        {
            throw new NotImplementedException();
        }

        public Task<string> PlaceTpSlOrderAsync(string symbol, decimal stopLoss, decimal takeProfit)
        {
            throw new NotImplementedException();
        }
    }
}