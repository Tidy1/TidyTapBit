﻿using Newtonsoft.Json;

namespace TidyTrader.ApiIntegration.Models.Responses.Account
{
    public class ChangeLeverageResponse
    {
        [JsonProperty("code")]
        public int Code { get; set; }

        [JsonProperty("data")]
        public List<ChangeLeverageItem> Data { get; set; }

        [JsonProperty("msg")]
        public string Message { get; set; }
    }

    public class ChangeLeverageItem
    {
        [JsonProperty("marginCoin")]
        public string MarginCoin { get; set; }

        [JsonProperty("leverage")]
        public int Leverage { get; set; }

        [JsonProperty("symbol")]
        public string Symbol { get; set; }
    }
}
