using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Graduation_Project.ViewModels.Analysis
{
    public class AnalysisSubmitRequest
    {
        [JsonPropertyName("personal_information")]
        public AnalysisPersonalInfoDto PersonalInformation { get; set; } = new();

        [JsonPropertyName("results")]
        public List<Dictionary<string, object>> Results { get; set; } = new();
    }
}
