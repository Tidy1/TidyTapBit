using Newtonsoft.Json;

using RestSharp;

using System.Security.Cryptography;
using System.Text;

using TidyTrader.ApiIntegration.Interfaces;

namespace TidyTrader.ApiIntegration.Models
{
    public class TapBitMarketData : IMarketData
    {
        private readonly RestClient _client;
        private readonly string _apiKey;
        private readonly string _apiSecret;

        public TapBitMarketData()
        {
            _client = new RestClient("https://openapi.tapbit.com/spot/");
            _apiKey = "1208f4a4096c834df9ee2b6c73482490";
            _apiSecret = "237c6796641c4fd5860b546f032ab43b";
        }

        private async Task<string> ExecuteRequestAsync(string endpoint, bool requiresAuth = false, string queryString = "")
        {
            string fullUrl = string.IsNullOrEmpty(queryString) ? endpoint : $"{endpoint}?{queryString}";
            var request = new RestRequest(fullUrl, Method.Get);
            request.AddHeader("Content-Type", "application/json");

            if (requiresAuth)
            {
                string timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
                string signature = GenerateSignature(queryString, timestamp);

                request.AddHeader("ACCESS-KEY", _apiKey);
                request.AddHeader("ACCESS-SIGN", signature);
                request.AddHeader("ACCESS-TIMESTAMP", timestamp);
            }

            var response = await _client.ExecuteAsync(request);
            if (!response.IsSuccessful)
            {
                throw new Exception($"API request failed: {response.StatusCode} - {response.Content}");
            }
            return response.Content;
        }

        private string GenerateSignature(string queryString, string timestamp)
        {
            string payload = timestamp + queryString;
            using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_apiSecret)))
            {
                byte[] hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
                return BitConverter.ToString(hash).Replace("-", "").ToLower();
            }
        }

        public async Task<DateTime> GetServerTimeAsync()
        {
            var response = await ExecuteRequestAsync("/api/spot/instruments/asset/list", true);
            dynamic result = JsonConvert.DeserializeObject(response);
            return DateTimeOffset.FromUnixTimeMilliseconds((long)result.serverTime).UtcDateTime;
        }

        public async Task<string> GetExchangeInfoAsync()
        {
            return await ExecuteRequestAsync("/perpetual/v1/public/exchangeInfo");
        }

        public async Task<string> GetOrderBookAsync(string symbol, int limit)
        {
            return await ExecuteRequestAsync($"/perpetual/v1/public/depth?symbol={symbol}&limit={limit}");
        }

        public async Task<string> GetKlineDataAsync(string symbol, string interval, int limit)
        {
            return await ExecuteRequestAsync($"/perpetual/v1/public/kline?symbol={symbol}&interval={interval}&limit={limit}");
        }

        public async Task<string> GetTickerAsync(string symbol)
        {
            return await ExecuteRequestAsync($"/perpetual/v1/public/ticker?symbol={symbol}");
        }

        public async Task<string> GetTickerListAsync()
        {
            return await ExecuteRequestAsync("/perpetual/v1/public/tickerBook");
        }

        public async Task<string> GetFundingRateAsync(string symbol)
        {
            return await ExecuteRequestAsync($"/perpetual/v1/public/fundingRate?symbol={symbol}");
        }

        public async Task<string> GetRecentTradesAsync(string symbol, int limit)
        {
            return await ExecuteRequestAsync($"/perpetual/v1/public/trades?symbol={symbol}&limit={limit}");
        }

        public Task<string> GetOrderBookAsync(string symbol, string limit)
        {
            throw new NotImplementedException();
        }

        public Task<string> GetKlineDataAsync(string symbol, string interval, string limit)
        {
            throw new NotImplementedException();
        }

        public Task<string> GetRecentTradesAsync(string? symbol = null, long? startTime = null, long? endTime = null, int? limit = null)
        {
            throw new NotImplementedException();
        }

        public decimal GetLivePrice(string symbol)
        {
            throw new NotImplementedException();
        }

        public decimal GetMovingAverage(string symbol, string period)
        {
            throw new NotImplementedException();
        }

        public decimal GetOrderBookSpread(string symbol)
        {
            throw new NotImplementedException();
        }

        public (decimal bidVolume, decimal askVolume) GetOrderBookVolumes(string symbol)
        {
            throw new NotImplementedException();
        }
    }
}
