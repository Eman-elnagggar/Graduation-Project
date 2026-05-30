using System.Text;
using System.Text.Json;
using Graduation_Project.Interfaces;
using Graduation_Project.Models;

namespace Graduation_Project.Services
{
    public class ChatbotService : IChatbotService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<ChatbotService> _logger;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public ChatbotService(HttpClient httpClient, ILogger<ChatbotService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<ChatbotApiResponse> GetReplyAsync(string message, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                throw new ArgumentException("Message is required.", nameof(message));
            }

            var payload = new ChatbotApiRequest { Message = message.Trim() };
            var json = JsonSerializer.Serialize(payload, JsonOptions);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");

            using var response = await _httpClient.PostAsync("chat", content, cancellationToken);
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Chatbot API returned {StatusCode}: {Body}", (int)response.StatusCode, responseBody);
                throw new InvalidOperationException("Chatbot API request failed.");
            }

            var result = JsonSerializer.Deserialize<ChatbotApiResponse>(responseBody, JsonOptions);
            if (result == null
                || string.IsNullOrWhiteSpace(result.Response)
                || string.IsNullOrWhiteSpace(result.RiskLevel)
                || string.IsNullOrWhiteSpace(result.Recommendation))
            {
                _logger.LogWarning("Chatbot API returned invalid payload: {Body}", responseBody);
                throw new InvalidOperationException("Chatbot API returned an invalid response.");
            }

            return result;
        }
    }
}
