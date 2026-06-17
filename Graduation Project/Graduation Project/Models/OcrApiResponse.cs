using System.Text.Json.Serialization;

namespace Graduation_Project.Models
{
    public class OcrApiResponse
    {
        [JsonPropertyName("results")]
        public OcrResults? Results { get; set; }
    }
}
