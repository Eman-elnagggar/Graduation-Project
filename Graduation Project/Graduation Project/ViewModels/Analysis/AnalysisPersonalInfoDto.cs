using System.Text.Json.Serialization;

namespace Graduation_Project.ViewModels.Analysis
{
    public class AnalysisPersonalInfoDto
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("age")]
        public int? Age { get; set; }

        [JsonPropertyName("trimester")]
        public int? Trimester { get; set; }

        [JsonPropertyName("week")]
        public int? Week { get; set; }

        [JsonPropertyName("baby_gender")]
        public string? BabyGender { get; set; }

        [JsonPropertyName("height")]
        public int? Height { get; set; }

        [JsonPropertyName("weight")]
        public int? Weight { get; set; }

        [JsonPropertyName("parity")]
        public int? Parity { get; set; }

        [JsonPropertyName("rbs_avg")]
        public int? RbsAverage { get; set; }

        [JsonPropertyName("avg_systolic")]
        public int? AvgSystolic { get; set; }

        [JsonPropertyName("avg_diastolic")]
        public int? AvgDiastolic { get; set; }

        [JsonPropertyName("dg_state")]
        public string? DgState { get; set; }

        [JsonPropertyName("risk_state")]
        public string? RiskState { get; set; }
    }
}
