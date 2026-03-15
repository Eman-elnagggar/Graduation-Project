using Graduation_Project.Services;
using Microsoft.AspNetCore.Mvc;
using Graduation_Project.Models;

namespace Graduation_Project.Controllers
{
    public class ProductScanController : Controller
    {
        private readonly ProductOcrClient _ocrClient;
        private readonly ILogger<ProductScanController> _logger;

        private static readonly HashSet<string> _allowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            "image/jpeg",
            "image/jpg",
            "image/png",
            "image/webp"
        };

        private const long MaxFileSizeBytes = 5 * 1024 * 1024; // 5 MB

        public ProductScanController(ProductOcrClient ocrClient, ILogger<ProductScanController> logger)
        {
            _ocrClient = ocrClient;
            _logger = logger;
        }

        // GET /ProductScan/Index
        [HttpGet]
        public IActionResult Index(int id = 0)
        {
            ViewData["Title"] = "Product Safety";
            ViewData["ActivePage"] = "Products";
            ViewData["PatientId"] = id;
            return View();
        }

        // POST /ProductScan/AnalyzeImage
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AnalyzeImage(IFormFile? image)
        {
            // ?? Validation ????????????????????????????????????????????????
            if (image == null || image.Length == 0)
                return BadRequest(new { error = "No image file provided." });

            if (!_allowedContentTypes.Contains(image.ContentType))
                return BadRequest(new { error = "Invalid file type. Please upload a JPG, PNG, or WebP image." });

            if (image.Length > MaxFileSizeBytes)
                return BadRequest(new { error = "File size exceeds the 5 MB limit." });

            // ?? Call OCR service ?????????????????????????????????????????
            try
            {
                var ocrResult = await _ocrClient.AnalyzeImageAsync(image);
                return BuildAnalysisResponse(ocrResult);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while analyzing product image.");
                return StatusCode(500, new { error = "An unexpected error occurred. Please try again later." });
            }
        }

        // POST /ProductScan/AnalyzeText
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AnalyzeText([FromForm] string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return BadRequest(new { error = "Please enter product ingredients first." });

            try
            {
                var ocrResult = await _ocrClient.AnalyzeTextAsync(text.Trim());
                return BuildAnalysisResponse(ocrResult);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while analyzing manual product text.");
                return StatusCode(500, new { error = "An unexpected error occurred. Please try again later." });
            }
        }

        private IActionResult BuildAnalysisResponse(OcrApiResponse? ocrResult)
        {
            if (ocrResult?.Results == null)
                return StatusCode(502, new { error = "The analysis service returned an unexpected response. Please try again." });

            var safe = ocrResult.Results.Safe ?? new List<string>();
            var risky = ocrResult.Results.Risky ?? new List<string>();
            var avoid = ocrResult.Results.Avoid ?? new List<string>();

            string status;
            if (avoid.Count > 0)
                status = "Unsafe";
            else if (risky.Count > 0)
                status = "NeedsReview";
            else
                status = "Safe";

            return Json(new
            {
                status,
                safe,
                risky,
                avoid
            });
        }
    }
}
