using System.Text.Json.Serialization;

namespace Graduation_Project.Models
{
    public class ChatbotApiRequest
    {
        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;
    }
}
