using System.Text;
using System.Text.Json;
using Graduation_Project.ViewModels.Analysis;

namespace Graduation_Project.Services
{
    public class AnalysisSubmitClient
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<AnalysisSubmitClient> _logger;
        // 502/503/504 mean the HF space is asleep/starting — wait long for it to wake.
        private static readonly HashSet<int> _wakeStatusCodes = new() { 502, 503, 504 };
        // 500 from this space is transient (the risk-prediction model intermittently
        // crashes/OOMs on the free tier and recovers). The same payload succeeds on a
        // quick retry, so retry 500 a few times with a short delay before giving up.
        private static readonly HashSet<int> _transientStatusCodes = new() { 500 };

        public AnalysisSubmitClient(IHttpClientFactory httpClientFactory, ILogger<AnalysisSubmitClient> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public async Task<AnalysisSubmitResponse?> SubmitAsync(AnalysisSubmitRequest request, CancellationToken cancellationToken)
        {
            // HF free-tier spaces sleep after inactivity and need up to 90s to wake.
            // We retry 503/502/504 with a 30s delay to give the space time to start.
            const int maxStatusRetries = 4;
            const int maxTransientRetries = 5;
            const int maxExceptionRetries = 2;
            int attempt = 0;
            int statusRetries = 0;
            int transientRetries = 0;
            int exceptionRetries = 0;

            while (true)
            {
                attempt++;
                try
                {
                    var client = _httpClientFactory.CreateClient("AnalysisSubmit");
                    var json = JsonSerializer.Serialize(request, SubmitJsonOptions());
                    _logger.LogInformation("Submitting analysis payload (attempt {Attempt}): {Payload}", attempt, json);
                    using var content = new StringContent(json, Encoding.UTF8, "application/json");

                    var response = await client.PostAsync("/submit-json", content, cancellationToken);

                    if (_wakeStatusCodes.Contains((int)response.StatusCode) && statusRetries < maxStatusRetries)
                    {
                        statusRetries++;
                        // Wait 30s to let the HF space wake up from sleep before retrying
                        _logger.LogWarning("Submit API returned {StatusCode} (wake-retry {Retry}/{Max}). Waiting 30s for service to start...", (int)response.StatusCode, statusRetries, maxStatusRetries);
                        await Task.Delay(30_000, cancellationToken);
                        continue;
                    }

                    if (_transientStatusCodes.Contains((int)response.StatusCode) && transientRetries < maxTransientRetries)
                    {
                        transientRetries++;
                        // The space's risk model crashes intermittently and recovers quickly — retry the
                        // same payload after a short, growing delay before treating it as a failure.
                        int delaySeconds = 3 + (transientRetries * 2);
                        _logger.LogWarning("Submit API returned {StatusCode} (transient-retry {Retry}/{Max}). Waiting {Delay}s before retrying...", (int)response.StatusCode, transientRetries, maxTransientRetries, delaySeconds);
                        await Task.Delay(delaySeconds * 1_000, cancellationToken);
                        continue;
                    }

                    if (!response.IsSuccessStatusCode)
                    {
                        var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                        _logger.LogWarning("Submit API failed with status {StatusCode}. Body: {Body}", (int)response.StatusCode, errorBody);
                        throw new InvalidOperationException($"The analysis service returned an error ({(int)response.StatusCode}). Please try again later.");
                    }

                    var jsonResponse = await response.Content.ReadAsStringAsync(cancellationToken);
                    _logger.LogDebug("Submit API raw response: {Json}", jsonResponse);

                    var result = JsonSerializer.Deserialize<AnalysisSubmitResponse>(jsonResponse, JsonOptions());
                    if (result == null)
                        throw new InvalidOperationException("The analysis service returned an unexpected empty response. Please try again.");

                    return result;
                }
                catch (InvalidOperationException)
                {
                    throw;
                }
                catch (Exception ex) when (exceptionRetries < maxExceptionRetries)
                {
                    exceptionRetries++;
                    int delayMs = exceptionRetries * 5_000;
                    _logger.LogWarning(ex, "Submit API call failed (exception-retry {Retry}/{Max}). Retrying in {Delay}ms...", exceptionRetries, maxExceptionRetries, delayMs);
                    await Task.Delay(delayMs, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Submit API call failed after {Attempts} attempts.", attempt);
                    throw new InvalidOperationException(
                        $"The analysis service is unavailable. Please try again in a few minutes. ({ex.Message})", ex);
                }
            }
        }

        private static JsonSerializerOptions JsonOptions() => new()
        {
            PropertyNameCaseInsensitive = true
        };

        private static JsonSerializerOptions SubmitJsonOptions() => new()
        {
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };
    }
}
