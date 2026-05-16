using System.Text;
using System.Text.Json;
using Graduation_Project.ViewModels.Analysis;

namespace Graduation_Project.Services
{
    public class AnalysisSubmitClient
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<AnalysisSubmitClient> _logger;
        private static readonly HashSet<int> _retryStatusCodes = new() { 502, 503, 504 };

        public AnalysisSubmitClient(IHttpClientFactory httpClientFactory, ILogger<AnalysisSubmitClient> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public async Task<AnalysisSubmitResponse?> SubmitAsync(AnalysisSubmitRequest request, CancellationToken cancellationToken)
        {
            const int maxRetries = 2;
            int attempt = 0;

            while (true)
            {
                attempt++;
                try
                {
                    var client = _httpClientFactory.CreateClient("AnalysisSubmit");
                    var json = JsonSerializer.Serialize(request);
                    _logger.LogInformation("Submitting analysis payload: {Payload}", json);
                    using var content = new StringContent(json, Encoding.UTF8, "application/json");

                    var response = await client.PostAsync("/submit-json", content, cancellationToken);

                    if (_retryStatusCodes.Contains((int)response.StatusCode) && attempt <= maxRetries)
                    {
                        int delayMs = (int)Math.Pow(2, attempt) * 500;
                        _logger.LogWarning("Submit API returned {StatusCode} (attempt {Attempt}/{Max}). Retrying in {Delay}ms...", (int)response.StatusCode, attempt, maxRetries, delayMs);
                        await Task.Delay(delayMs, cancellationToken);
                        continue;
                    }

                    if (!response.IsSuccessStatusCode)
                    {
                        var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                        _logger.LogWarning("Submit API failed with status {StatusCode}. Body: {Body}", (int)response.StatusCode, errorBody);
                        throw new InvalidOperationException($"Submit API error: {(int)response.StatusCode} - {errorBody}");
                    }

                    var jsonResponse = await response.Content.ReadAsStringAsync(cancellationToken);
                    _logger.LogDebug("Submit API raw response: {Json}", jsonResponse);
                    return JsonSerializer.Deserialize<AnalysisSubmitResponse>(jsonResponse, JsonOptions());
                }
                catch (Exception ex) when (attempt <= maxRetries && ex is not InvalidOperationException)
                {
                    int delayMs = (int)Math.Pow(2, attempt) * 500;
                    _logger.LogWarning(ex, "Submit API call failed (attempt {Attempt}/{Max}). Retrying in {Delay}ms...", attempt, maxRetries, delayMs);
                    await Task.Delay(delayMs, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Submit API call failed after {Attempts} attempts.", attempt);
                    return null;
                }
            }
        }

        private static JsonSerializerOptions JsonOptions() => new()
        {
            PropertyNameCaseInsensitive = true
        };
    }
}
