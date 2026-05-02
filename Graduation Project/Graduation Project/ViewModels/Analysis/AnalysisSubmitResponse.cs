using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Graduation_Project.ViewModels.Analysis
{
    public class AnalysisSubmitResponse
    {
        [JsonPropertyName("result_1")]
        public Dictionary<string, object>? PersonalInfo { get; set; }

        [JsonPropertyName("result_2")]
        public List<Dictionary<string, object>>? TestResults { get; set; }

        [JsonPropertyName("result_3")]
        public System.Text.Json.JsonElement? RiskPrediction { get; set; }

        [JsonPropertyName("result_4")]
        public string? Report { get; set; }

        [JsonPropertyName("result_5")]
        public List<string>? Alerts { get; set; }
    }
}
