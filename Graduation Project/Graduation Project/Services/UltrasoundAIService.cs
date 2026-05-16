using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Graduation_Project.Services
{
    public class UltrasoundAIService : IUltrasoundAIService
    {
        private readonly HttpClient _httpClient;

        public UltrasoundAIService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<UltrasoundAIResult> AnalyzeAsync(Stream imageStream, string fileName, CancellationToken cancellationToken = default)
        {
            if (imageStream == null)
            {
                throw new ArgumentNullException(nameof(imageStream));
            }

            byte[] imageBytes;
            await using (var memory = new MemoryStream())
            {
                await imageStream.CopyToAsync(memory, cancellationToken);
                imageBytes = memory.ToArray();
            }

            using var form = new MultipartFormDataContent();
            var contentType = GetContentType(fileName);
            var fileContent = new ByteArrayContent(imageBytes);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
            form.Add(fileContent, "file", fileName);

            var imageContent = new ByteArrayContent(imageBytes);
            imageContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
            form.Add(imageContent, "image", fileName);

            using var response = await _httpClient.PostAsync("fetal", form, cancellationToken);
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException($"AI service returned {(int)response.StatusCode}: {responseBody}");
            }

            using var jsonDoc = JsonDocument.Parse(responseBody);
            var root = jsonDoc.RootElement;

            string base64Image = null;
            if (root.TryGetProperty("processed_image", out var processedImageElement))
            {
                base64Image = processedImageElement.GetString();
            }
            else if (root.TryGetProperty("image", out var imageElement))
            {
                base64Image = imageElement.GetString();
            }
            else if (root.TryGetProperty("result", out var resultElement))
            {
                base64Image = resultElement.GetString();
            }

            if (string.IsNullOrWhiteSpace(base64Image))
            {
                throw new InvalidOperationException("AI response did not contain a processed image.");
            }

            byte[] processedBytes;
            try
            {
                processedBytes = Convert.FromBase64String(base64Image);
            }
            catch (FormatException ex)
            {
                throw new InvalidOperationException("Invalid Base64 image returned from AI service.", ex);
            }

            var result = new UltrasoundAIResult
            {
                ProcessedImageBytes = processedBytes,
                RawJson = responseBody
            };

            if (root.TryGetProperty("prediction", out var predictionElement))
            {
                result.Prediction = predictionElement.GetString();
            }

            if (string.IsNullOrWhiteSpace(result.Prediction) && root.TryGetProperty("risk", out var riskElement))
            {
                result.Prediction = riskElement.GetString();
            }

            if (root.TryGetProperty("confidence", out var confidenceElement) && confidenceElement.TryGetDouble(out var confidence))
            {
                result.ConfidenceScore = confidence;
            }

            if (root.TryGetProperty("thickness_mm", out var thicknessElement))
            {
                if (thicknessElement.TryGetDouble(out var thicknessValue))
                {
                    result.ThicknessMm = thicknessValue;
                }
                else if (thicknessElement.ValueKind == JsonValueKind.String
                    && double.TryParse(thicknessElement.GetString(), out var thicknessFromString))
                {
                    result.ThicknessMm = thicknessFromString;
                }
            }

            return result;
        }

        private static string GetContentType(string fileName)
        {
            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            return extension switch
            {
                ".jpg" => "image/jpeg",
                ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                _ => "application/octet-stream"
            };
        }
    }
}
