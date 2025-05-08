using Newtonsoft.Json;

namespace TidyTrader.ApiIntegration.Models.Responses.Position
{
    public class PositionTier
    {
        [JsonProperty("symbol")]
        public string Symbol { get; set; }

        [JsonProperty("level")]
        public int Level { get; set; }

        [JsonProperty("startValue")]
        public string StartValue { get; set; }

        [JsonProperty("endValue")]
        public string EndValue { get; set; }

        [JsonProperty("leverage")]
        public int Leverage { get; set; }

        [JsonProperty("maintenanceMarginRate")]
        public string MaintenanceMarginRate { get; set; }
    }

    public class PositionTiersResponse
    {
        [JsonProperty("data")]
        public List<PositionTier> Data { get; set; }
    }

}
