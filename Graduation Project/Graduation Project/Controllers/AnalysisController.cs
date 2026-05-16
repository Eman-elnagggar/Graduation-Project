using Graduation_Project.Interfaces;
using Graduation_Project.ViewModels.Analysis;
using Graduation_Project.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Graduation_Project.Controllers
{
    [ApiController]
    [Route("api/analysis")]
    [Authorize(Roles = "Patient")]
    public class AnalysisController : ControllerBase
    {
        private readonly IAnalysisService _analysisService;
        private readonly IBackgroundJobScheduler _backgroundJobScheduler;
        private readonly ILogger<AnalysisController> _logger;
        private readonly ILabTest _labTestRepository;

        public AnalysisController(
            IAnalysisService analysisService,
            IBackgroundJobScheduler backgroundJobScheduler,
            ILogger<AnalysisController> logger,
            ILabTest labTestRepository)
        {
            _analysisService = analysisService;
            _backgroundJobScheduler = backgroundJobScheduler;
            _logger = logger;
            _labTestRepository = labTestRepository;
        }

        [HttpPost]
        [RequestSizeLimit(10 * 1024 * 1024)]
        public async Task<IActionResult> UploadAsync([FromForm] AnalysisUploadRequest request, CancellationToken cancellationToken)
        {
            try
            {
                if (request.Image == null || request.Image.Length == 0)
                    return BadRequest(new { error = "No image file provided." });

                if (request.PatientId <= 0)
                    return BadRequest(new { error = "Invalid patient id." });

                if (string.IsNullOrWhiteSpace(request.TestType))
                    return BadRequest(new { error = "Test type is required." });

                var response = await _analysisService.UploadAndExtractAsync(request, cancellationToken);
                return Ok(response);
            }
            catch (InvalidOperationException ex) when (
                ex.Message.Contains("OCR service unavailable", StringComparison.OrdinalIgnoreCase)
                || ex.Message.Contains("OCR API error", StringComparison.OrdinalIgnoreCase)
                || ex.Message.Contains("OCR service returned no values", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogError(ex, "External OCR service error while uploading analysis data.");
                return StatusCode(502, new { error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Validation failed while uploading analysis data.");
                return BadRequest(new { error = ex.Message });
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogWarning(ex, "OCR API timed out.");
                return StatusCode(504, new { error = "The analysis service timed out. Please try again." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to upload and extract analysis data.");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPost("{id:int}/confirm")]
        public async Task<IActionResult> ConfirmAsync(int id, [FromBody] AnalysisConfirmRequest request, CancellationToken cancellationToken)
        {
            try
            {
                if (request.Values == null || request.Values.Count == 0)
                    return BadRequest(new { error = "No values provided for confirmation." });

                var response = await _analysisService.ConfirmAsync(id, request, cancellationToken);
                _backgroundJobScheduler.EnqueueAnalysis(id);
                return Ok(response);
            }
            catch (InvalidOperationException ex) when (
                ex.Message.Contains("Confirm service unavailable", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogError(ex, "External confirm service error.");
                return StatusCode(502, new { error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Validation failed while confirming analysis data.");
                return BadRequest(new { error = ex.Message });
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogWarning(ex, "Confirm API timed out.");
                return StatusCode(504, new { error = "The confirmation service timed out. Please try again." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to confirm analysis for lab test {LabTestId}.", id);
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetAsync(int id, CancellationToken cancellationToken)
        {
            var result = await _analysisService.GetAnalysisResultAsync(id, cancellationToken);
            if (result == null)
            {
                return NotFound();
            }

            return Ok(result);
        }

        [HttpGet("patient/{patientId:int}/tests")]
        public IActionResult GetPatientTests(int patientId)
        {
            if (patientId <= 0)
                return BadRequest(new { error = "Invalid patient id." });

            var testTypeNames = new System.Collections.Generic.Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase)
            {
                { "cbc",       "CBC (Complete Blood Count)" },
                { "urinalysis","Urinalysis" },
                { "tsh",       "TSH (Thyroid)" },
                { "ferritin",  "Ferritin" },
                { "fbg",       "Fasting Blood Glucose" },
                { "hba1c",     "HbA1c (Sugar Test)" },
                { "hcv",       "HCV (Hepatitis C)" },
                { "hbsag",     "HBsAg (Hepatitis B)" },
                { "bloodgroup","Blood Group" }
            };

            string ResolveName(string? testType, string? testName) =>
                testTypeNames.TryGetValue(testType ?? "", out var n) ? n
                : (!string.IsNullOrWhiteSpace(testName) ? testName : testType ?? "Unknown");

            var tests = _labTestRepository.GetLabTestsByPatientId(patientId);

            var grouped = tests
                .Where(t => t.ReportID.HasValue)
                .GroupBy(t => t.ReportID!.Value)
                .Select(g =>
                {
                    var sorted      = g.OrderBy(t => t.UploadDate).ToList();
                    var maxDate     = sorted.Max(t => t.UploadDate);
                    var report      = sorted.Select(t => t.TestReport).FirstOrDefault(r => r != null);
                    var namesList   = sorted.Select(t => ResolveName(t.TestType, t.TestName)).ToList();
                    var imageList   = sorted
                        .Where(t => !string.IsNullOrWhiteSpace(t.ImagePath))
                        .Select(t => new { testName = ResolveName(t.TestType, t.TestName), path = t.ImagePath })
                        .ToList();
                    var viewTestId  = sorted.FirstOrDefault(t => t.TestReport?.OverallStatus != null)?.LabTestID
                                     ?? sorted.Last().LabTestID;
                    return new
                    {
                        maxDate,
                        reportId      = g.Key,
                        labTestId     = viewTestId,
                        uploadDate    = maxDate.ToString("MMM dd, yyyy"),
                        testNames     = namesList,
                        displayName   = string.Join(" · ", namesList),
                        status        = report?.AnalysisStatus ?? sorted.Last().AnalysisStatus ?? "Pending",
                        overallStatus = report?.OverallStatus ?? "",
                        hasReport     = report?.OverallStatus != null,
                        images        = imageList
                    };
                })
                .OrderByDescending(g => g.maxDate)
                .Select(g => new
                {
                    g.reportId,
                    g.labTestId,
                    g.uploadDate,
                    g.testNames,
                    g.displayName,
                    g.status,
                    g.overallStatus,
                    g.hasReport,
                    g.images
                })
                .ToList();

            return Ok(grouped);
        }
    }
}