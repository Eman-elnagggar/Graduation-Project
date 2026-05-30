using System.Text.Json.Serialization;

namespace Graduation_Project.Models
{
    public class ChatbotAskRequest
    {
        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        [JsonPropertyName("patientId")]
        public int? PatientId { get; set; }
    }
}
