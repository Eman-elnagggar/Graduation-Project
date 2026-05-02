using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Graduation_Project.ViewModels.Analysis;

namespace Graduation_Project.Services
{
    public class AnalysisOcrClient
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<AnalysisOcrClient> _logger;
        private static readonly HashSet<int> _retryStatusCodes = new() { 502, 503, 504 };

        public AnalysisOcrClient(IHttpClientFactory httpClientFactory, ILogger<AnalysisOcrClient> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public async Task<AnalysisOcrResponse?> AnalyzeImageAsync(IFormFile image, string? testType, CancellationToken cancellationToken)
        {
            const int maxRetries = 2;
            int attempt = 0;

            if (image.Length > 5 * 1024 * 1024)
                throw new InvalidOperationException("Image size exceeds 5 MB limit.");

            using var ms = new MemoryStream();
            await image.CopyToAsync(ms, cancellationToken);
            var bytes = ms.ToArray();

            while (true)
            {
                attempt++;
                try
                {
                    var client = _httpClientFactory.CreateClient("AnalysisOcr");
                    using var content = new MultipartFormDataContent();
                    var imageContent = new ByteArrayContent(bytes);
                    imageContent.Headers.ContentType = new MediaTypeHeaderValue(image.ContentType);
                    content.Add(imageContent, "image", image.FileName);

                    var fileContent = new ByteArrayContent(bytes);
                    fileContent.Headers.ContentType = new MediaTypeHeaderValue(image.ContentType);
                    content.Add(fileContent, "file", image.FileName);

                    if (!string.IsNullOrWhiteSpace(testType))
                    {
                        content.Add(new StringContent(testType), "test_type");
                        content.Add(new StringContent(testType), "testType");
                    }

                    var response = await client.PostAsync("/analyze", content, cancellationToken);

                    if (_retryStatusCodes.Contains((int)response.StatusCode) && attempt <= maxRetries)
                    {
                        int delayMs = (int)Math.Pow(2, attempt) * 500;
                        _logger.LogWarning("OCR API returned {StatusCode} (attempt {Attempt}/{Max}). Retrying in {Delay}ms...", (int)response.StatusCode, attempt, maxRetries, delayMs);
                        await Task.Delay(delayMs, cancellationToken);
                        continue;
                    }

                    if (!response.IsSuccessStatusCode)
                    {
                        var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                        _logger.LogWarning("OCR API failed with status {StatusCode}. Body: {Body}", (int)response.StatusCode, errorBody);
                        throw new InvalidOperationException($"OCR API error: {(int)response.StatusCode} - {errorBody}");
                    }
                    var json = await response.Content.ReadAsStringAsync(cancellationToken);
                    _logger.LogDebug("OCR API raw response: {Json}", json);
                    return ParseOcrResponse(json);
                }
                catch (Exception ex) when (attempt <= maxRetries)
                {
                    int delayMs = (int)Math.Pow(2, attempt) * 500;
                    _logger.LogWarning(ex, "OCR API call failed (attempt {Attempt}/{Max}). Retrying in {Delay}ms...", attempt, maxRetries, delayMs);
                    await Task.Delay(delayMs, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "OCR API call failed after {Attempts} attempts.", attempt);
                    throw new InvalidOperationException(
                        $"OCR service unavailable after {attempt} attempts: {ex.Message}", ex);
                }
            }
        }

        public async Task<Dictionary<string, object>?> ConfirmAsync(string testName, Dictionary<string, object> values, CancellationToken cancellationToken)
        {
            const int maxRetries = 2;
            int attempt = 0;

            while (true)
            {
                attempt++;
                try
                {
                    var client = _httpClientFactory.CreateClient("AnalysisConfirm");
                    var payload = new Dictionary<string, object>(values, StringComparer.OrdinalIgnoreCase)
                    {
                        ["test_name"] = testName
                    };
                    var json = JsonSerializer.Serialize(payload);
                    using var content = new StringContent(json, Encoding.UTF8, "application/json");

                    var response = await client.PostAsync("/confirm", content, cancellationToken);

                    if (_retryStatusCodes.Contains((int)response.StatusCode) && attempt <= maxRetries)
                    {
                        int delayMs = (int)Math.Pow(2, attempt) * 500;
                        _logger.LogWarning("Confirm API returned {StatusCode} (attempt {Attempt}/{Max}). Retrying in {Delay}ms...", (int)response.StatusCode, attempt, maxRetries, delayMs);
                        await Task.Delay(delayMs, cancellationToken);
                        continue;
                    }

                    if (!response.IsSuccessStatusCode)
                    {
                        var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                        _logger.LogWarning("Confirm API failed with status {StatusCode}. Body: {Body}", (int)response.StatusCode, errorBody);
                        throw new InvalidOperationException($"Confirm API error: {(int)response.StatusCode} - {errorBody}");
                    }

                    var jsonResponse = await response.Content.ReadAsStringAsync(cancellationToken);
                    _logger.LogDebug("Confirm API raw response: {Json}", jsonResponse);
                    var result = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonResponse, JsonOptions());
                    if (result != null && result.TryGetValue("error", out var errorValue) && !string.IsNullOrWhiteSpace(errorValue?.ToString()))
                    {
                        throw new InvalidOperationException($"Confirm API error: {errorValue}");
                    }
                    return result;
                }
                catch (Exception ex) when (attempt <= maxRetries && ex is not InvalidOperationException)
                {
                    int delayMs = (int)Math.Pow(2, attempt) * 500;
                    _logger.LogWarning(ex, "Confirm API call failed (attempt {Attempt}/{Max}). Retrying in {Delay}ms...", attempt, maxRetries, delayMs);
                    await Task.Delay(delayMs, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Confirm API call failed after {Attempts} attempts.", attempt);
                    if (ex is InvalidOperationException)
                        throw;
                    throw new InvalidOperationException(
                        $"Confirm service unavailable after {attempt} attempts: {ex.Message}", ex);
                }
            }
        }

        private static AnalysisOcrResponse? ParseOcrResponse(string json)
        {
            var document = JsonDocument.Parse(json);
            var root = document.RootElement;

            string? testName = null;
            decimal? confidence = null;
            var values = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

            foreach (var property in root.EnumerateObject())
            {
                if (property.NameEquals("test_name"))
                {
                    testName = property.Value.GetString();
                    continue;
                }

                if (property.NameEquals("confidence"))
                {
                    if (property.Value.ValueKind == JsonValueKind.Number && property.Value.TryGetDecimal(out var conf))
                    {
                        confidence = conf;
                    }
                    else
                    {
                        var confText = property.Value.GetString();
                        if (decimal.TryParse(confText, out var confValue))
                        {
                            confidence = confValue;
                        }
                    }
                    continue;
                }

                values[property.Name] = JsonElementToObject(property.Value);
            }

            return new AnalysisOcrResponse
            {
                TestName = testName ?? string.Empty,
                Confidence = confidence,
                Values = values
            };
        }

        private static object JsonElementToObject(JsonElement element)
        {
            return element.ValueKind switch
            {
                JsonValueKind.String => element.GetString() ?? string.Empty,
                JsonValueKind.Number => element.TryGetDecimal(out var value) ? value : element.GetDouble(),
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.Object => JsonSerializer.Deserialize<Dictionary<string, object>>(element.GetRawText(), JsonOptions()) ?? new Dictionary<string, object>(),
                JsonValueKind.Array => JsonSerializer.Deserialize<List<object>>(element.GetRawText(), JsonOptions()) ?? new List<object>(),
                _ => string.Empty
            };
        }

        private static JsonSerializerOptions JsonOptions() => new()
        {
            PropertyNameCaseInsensitive = true
        };
    }
}
