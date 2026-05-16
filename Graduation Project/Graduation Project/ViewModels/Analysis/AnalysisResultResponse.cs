using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Graduation_Project.ViewModels.Analysis
{
    public class AnalysisResultResponse
    {
        public string Status { get; set; } = string.Empty;

        [JsonPropertyName("personalInfo")]
        public Dictionary<string, object>? PersonalInfo { get; set; }

        [JsonPropertyName("tests")]
        public List<Dictionary<string, object>> Tests { get; set; } = new();

        [JsonPropertyName("riskPrediction")]
        public JsonElement? RiskPrediction { get; set; }

        [JsonPropertyName("report")]
        public string? Report { get; set; }

        [JsonPropertyName("alerts")]
        public List<string> Alerts { get; set; } = new();
    }
}
