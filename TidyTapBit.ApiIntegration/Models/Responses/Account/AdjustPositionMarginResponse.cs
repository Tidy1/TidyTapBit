using Newtonsoft.Json;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TidyTrader.ApiIntegration.Models.Responses.Account
{
    public class AdjustPositionMarginResponse
    {
        [JsonProperty("code")]
        public int Code { get; set; }

        [JsonProperty("data")]
        public string Data { get; set; }

        [JsonProperty("msg")]
        public string Message { get; set; }
    }
}
