using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Graduation_Project.Models
{
    public class OcrResults
    {
        [JsonPropertyName("safe")]
        public List<string> Safe { get; set; } = new();

        [JsonPropertyName("risky")]
        public List<string> Risky { get; set; } = new();

        [JsonPropertyName("avoid")]
        public List<string> Avoid { get; set; } = new();
    }
}
