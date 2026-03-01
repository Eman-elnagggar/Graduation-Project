using Graduation_Project.Services;
using Microsoft.AspNetCore.Mvc;

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

                if (ocrResult?.Results == null)
                    return StatusCode(502, new { error = "The analysis service returned an unexpected response. Please try again." });

                var results = ocrResult.Results;

                // ?? Determine status ?????????????????????????????????????
                string status;
                if (results.Avoid.Count > 0)
                    status = "Unsafe";
                else if (results.Risky.Count > 0)
                    status = "NeedsReview";
                else
                    status = "Safe";

                return Json(new
                {
                    status,
                    safe  = results.Safe,
                    risky = results.Risky,
                    avoid = results.Avoid
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while analyzing product image.");
                return StatusCode(500, new { error = "An unexpected error occurred. Please try again later." });
            }
        }
    }
}
