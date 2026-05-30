using System.Text.Json.Serialization;

namespace Graduation_Project.Models
{
    public class ChatbotClearRequest
    {
        [JsonPropertyName("patientId")]
        public int? PatientId { get; set; }
    }
}
