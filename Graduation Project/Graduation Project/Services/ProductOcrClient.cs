using Graduation_Project.Models;
using System.Text.Json;

namespace Graduation_Project.Services
{
    public class ProductOcrClient
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<ProductOcrClient> _logger;

        private static readonly HashSet<int> _retryStatusCodes = new() { 502, 503, 504 };

        public ProductOcrClient(IHttpClientFactory httpClientFactory, ILogger<ProductOcrClient> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public async Task<OcrApiResponse?> AnalyzeImageAsync(IFormFile image)
        {
            const int maxRetries = 2;
            int attempt = 0;

            while (true)
            {
                attempt++;
                try
                {
                    var client = _httpClientFactory.CreateClient("ProductOcr");

                    // Copy the stream into a memory buffer so it can be re-read on retries
                    using var ms = new MemoryStream();
                    await image.CopyToAsync(ms);
                    ms.Position = 0;

                    using var content = new MultipartFormDataContent();
                    var streamContent = new StreamContent(ms);
                    streamContent.Headers.ContentType =
                        new System.Net.Http.Headers.MediaTypeHeaderValue(image.ContentType);
                    content.Add(streamContent, "image", image.FileName);

                    var response = await client.PostAsync("/analyze-image", content);

                    if (_retryStatusCodes.Contains((int)response.StatusCode) && attempt <= maxRetries)
                    {
                        int delayMs = (int)Math.Pow(2, attempt) * 500; // 1s, 2s
                        _logger.LogWarning(
                            "OCR API returned {StatusCode} (attempt {Attempt}/{Max}). Retrying in {Delay}ms...",
                            (int)response.StatusCode, attempt, maxRetries, delayMs);
                        await Task.Delay(delayMs);
                        ms.Position = 0;
                        continue;
                    }

                    response.EnsureSuccessStatusCode();

                    var json = await response.Content.ReadAsStringAsync();
                    _logger.LogDebug("OCR API raw response: {Json}", json);

                    var result = JsonSerializer.Deserialize<OcrApiResponse>(json);
                    return result;
                }
                catch (Exception ex) when (attempt <= maxRetries)
                {
                    int delayMs = (int)Math.Pow(2, attempt) * 500;
                    _logger.LogWarning(ex,
                        "OCR API call failed (attempt {Attempt}/{Max}). Retrying in {Delay}ms...",
                        attempt, maxRetries, delayMs);
                    await Task.Delay(delayMs);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "OCR API call failed after {Attempts} attempts.", attempt);
                    return null;
                }
            }
        }

        public async Task<OcrApiResponse?> AnalyzeTextAsync(string text)
        {
            const int maxRetries = 2;
            int attempt = 0;

            while (true)
            {
                attempt++;
                try
                {
                    var client = _httpClientFactory.CreateClient("ProductOcr");

                    using var content = new FormUrlEncodedContent(new Dictionary<string, string>
                    {
                        ["text"] = text
                    });

                    var response = await client.PostAsync("/analyze-text", content);

                    if (_retryStatusCodes.Contains((int)response.StatusCode) && attempt <= maxRetries)
                    {
                        int delayMs = (int)Math.Pow(2, attempt) * 500; // 1s, 2s
                        _logger.LogWarning(
                            "OCR text API returned {StatusCode} (attempt {Attempt}/{Max}). Retrying in {Delay}ms...",
                            (int)response.StatusCode, attempt, maxRetries, delayMs);
                        await Task.Delay(delayMs);
                        continue;
                    }

                    response.EnsureSuccessStatusCode();

                    var json = await response.Content.ReadAsStringAsync();
                    _logger.LogDebug("OCR text API raw response: {Json}", json);

                    var result = JsonSerializer.Deserialize<OcrApiResponse>(json);
                    return result;
                }
                catch (Exception ex) when (attempt <= maxRetries)
                {
                    int delayMs = (int)Math.Pow(2, attempt) * 500;
                    _logger.LogWarning(ex,
                        "OCR text API call failed (attempt {Attempt}/{Max}). Retrying in {Delay}ms...",
                        attempt, maxRetries, delayMs);
                    await Task.Delay(delayMs);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "OCR text API call failed after {Attempts} attempts.", attempt);
                    return null;
                }
            }
        }
    }
}
