using Newtonsoft.Json;

namespace TidyTrader.ApiIntegration.Models.Responses.Trade
{
    public class FlashClosePositionData
    {
        [JsonProperty("positionId")]
        public string PositionId { get; set; }
    }

    public class FlashClosePositionResponse
    {
        [JsonProperty("code")]
        public int Code { get; set; }

        [JsonProperty("data")]
        public FlashClosePositionData Data { get; set; }

        [JsonProperty("msg")]
        public string Message { get; set; }
    }

}
