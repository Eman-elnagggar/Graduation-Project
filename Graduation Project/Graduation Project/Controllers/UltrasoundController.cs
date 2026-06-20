using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Security.Claims;
using Graduation_Project.Data;
using Graduation_Project.Interfaces;
using Graduation_Project.Models;
using Graduation_Project.Services;
using Graduation_Project.ViewModels.Ultrasound;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;

namespace Graduation_Project.Controllers
{
    [Authorize(Roles = "Doctor")]
    public class UltrasoundController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IPatientDoctor _patientDoctorRepository;
        private readonly IUltrasoundImage _ultrasoundRepository;
        private readonly IUltrasoundAIService _aiService;
        private readonly UltrasoundImageStorage _storage;
        private readonly IPatientNotificationService _notifications;
        private readonly IAlert _alertRepository;
        private readonly IPatient _patientRepository;
        private readonly ILogger<UltrasoundController> _logger;

        public UltrasoundController(
            AppDbContext context,
            IPatientDoctor patientDoctorRepository,
            IUltrasoundImage ultrasoundRepository,
            IUltrasoundAIService aiService,
            UltrasoundImageStorage storage,
            IPatientNotificationService notifications,
            IAlert alertRepository,
            IPatient patientRepository,
            ILogger<UltrasoundController> logger)
        {
            _context = context;
            _patientDoctorRepository = patientDoctorRepository;
            _ultrasoundRepository = ultrasoundRepository;
            _aiService = aiService;
            _storage = storage;
            _notifications = notifications;
            _alertRepository = alertRepository;
            _patientRepository = patientRepository;
            _logger = logger;
        }

        // ── GET: Upload ────────────────────────────────────────────────────────
        [HttpGet]
        public IActionResult Upload(int id, int? patientId)
        {
            var accessResult = TryResolveDoctor(id, out var doctor);
            if (accessResult != null) return accessResult;

            var patients = _patientDoctorRepository
                .GetApprovedByDoctor(doctor!.DoctorID)
                .Select(pd => pd.Patient)
                .Where(p => p != null)
                .ToList();

            var vm = new UltrasoundUploadViewModel
            {
                DoctorId = doctor.DoctorID,
                PatientId = patientId ?? 0,
                Patients = patients,
                AnalyzeWithAI = true
            };

            return View(vm);
        }

        // ── POST: Upload ───────────────────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(15 * 1024 * 1024)]
        [RequestFormLimits(MultipartBodyLengthLimit = 15 * 1024 * 1024)]
        public async Task<IActionResult> Upload(UltrasoundUploadViewModel model, CancellationToken cancellationToken)
        {
            var accessResult = TryResolveDoctor(model.DoctorId, out var doctor, true);
            if (accessResult != null) return accessResult;

            var patients = _patientDoctorRepository
                .GetApprovedByDoctor(doctor!.DoctorID)
                .Select(pd => pd.Patient)
                .Where(p => p != null)
                .ToList();

            model.Patients = patients;

            if (model.PatientId == 0 || !patients.Any(p => p.PatientID == model.PatientId))
                ModelState.AddModelError(nameof(model.PatientId), "Please select a valid patient.");

            if (!_storage.IsValid(model.ImageFile, out var errorMessage))
                ModelState.AddModelError(nameof(model.ImageFile), errorMessage);

            if (!ModelState.IsValid) return View(model);

            var originalPath = await _storage.SaveOriginalAsync(model.ImageFile!, cancellationToken);

            var record = new UltrasoundImage
            {
                PatientID = model.PatientId,
                DoctorID = doctor!.DoctorID,
                UploadDate = DateTime.Now,
                OriginalImagePath = originalPath,
                ImagePath = originalPath,
                AI_Result_JSON = string.Empty,
                DetectedAnomaly = string.Empty,
                DoctorComments = model.DoctorNote?.Trim() ?? string.Empty,
                Prediction = string.Empty,
                ResultImagePath = string.Empty,
                IsPatientUploaded = false
            };

            // ── Mode A: Save with note only (no AI) ────────────────────────────
            if (!model.AnalyzeWithAI)
            {
                record.Status = UltrasoundStatus.Completed;
                _ultrasoundRepository.Add(record);
                _ultrasoundRepository.Save();

                var noteDoctorName = BuildDoctorDisplayName(doctor!);

                // Bell notification + phone push so the patient knows a new scan was added.
                _notifications.Notify(model.PatientId,
                    "New Ultrasound Scan",
                    $"{noteDoctorName} uploaded a new ultrasound scan to your records. View it in your Medical History.",
                    PatientNotificationTypes.Ultrasound,
                    "/PatientMedicalHistory/MedicalHistory/" + model.PatientId);

                // Surface the new scan on the patient's Alerts page.
                AddUltrasoundAlert(model.PatientId, noteDoctorName, record);

                TempData["SuccessMessage"] = "Ultrasound image saved successfully with your note.";
                return RedirectToAction(nameof(History),
                    new { id = doctor.DoctorID, patientId = model.PatientId });
            }

            // ── Mode B: AI analysis ────────────────────────────────────────────
            record.Status = UltrasoundStatus.Processing;
            _ultrasoundRepository.Add(record);
            _ultrasoundRepository.Save();

            bool analysisSucceeded = false;

            try
            {
                await using var stream = model.ImageFile!.OpenReadStream();
                var aiResult = await _aiService.AnalyzeAsync(stream, model.ImageFile.FileName, cancellationToken);

                record.ResultImagePath = await _storage.SaveResultAsync(aiResult.ProcessedImageBytes, ".png", cancellationToken);
                record.Prediction = aiResult.Prediction ?? string.Empty;
                record.ConfidenceScore = aiResult.ConfidenceScore;
                record.AI_Result_JSON = aiResult.RawJson ?? string.Empty;
                record.DetectedAnomaly = IsUltrasoundAnomaly(record.Prediction) ? record.Prediction : string.Empty;
                record.Status = UltrasoundStatus.Completed;
                analysisSucceeded = true;
            }
            catch (Exception ex)
            {
                // Keep the technical detail in the logs, not in AI_Result_JSON (which is meant to
                // hold the model's JSON output and would otherwise carry a raw exception string).
                _logger.LogError(ex, "Ultrasound AI analysis failed for image {ImageId}.", record.ImageID);
                record.Status = UltrasoundStatus.Failed;
                record.AI_Result_JSON = string.Empty;

                TempData["ErrorMessage"] = "AI analysis is currently unavailable. The image has been saved and can be viewed in the patient's history.";
            }

            _ultrasoundRepository.Update(record);
            _ultrasoundRepository.Save();

            if (analysisSucceeded)
            {
                string doctorName = doctor.User != null
                    ? $"Dr. {doctor.User.FirstName} {doctor.User.LastName}".Trim()
                    : "Your doctor";

                string prediction = string.IsNullOrWhiteSpace(record.Prediction)
                    ? "result is ready" : record.Prediction;

                // Always: a "ready" status notification in the patient's bell.
                _notifications.Notify(model.PatientId,
                    "Ultrasound Analysis Ready",
                    $"{doctorName} uploaded and analyzed an ultrasound scan for you. Result: {prediction}. View the full result in your Medical History.",
                    PatientNotificationTypes.Ultrasound,
                    "/PatientMedicalHistory/MedicalHistory/" + model.PatientId);

                // Every analyzed ultrasound result also surfaces on the Alerts page
                // (info for a clear result, warning/danger when an anomaly is flagged).
                AddUltrasoundAlert(model.PatientId, doctorName, record);
            }

            return RedirectToAction(nameof(Result), new { id = record.ImageID });
        }

        // The AI returns a free-text prediction with no explicit normal/abnormal flag,
        // so anything that is not clearly a "clear/normal" result is treated as a finding.
        private static readonly string[] _clearPredictionKeywords =
            { "normal", "healthy", "no anomaly", "no abnormal", "negative", "clear", "low risk", "benign", "none" };

        private static readonly string[] _highRiskPredictionKeywords =
            { "high", "critical", "severe", "malignant", "danger" };

        private static bool IsUltrasoundAnomaly(string? prediction)
        {
            if (string.IsNullOrWhiteSpace(prediction)) return false;
            var p = prediction.Trim().ToLowerInvariant();
            return !_clearPredictionKeywords.Any(k => p.Contains(k));
        }

        private static bool IsHighRiskPrediction(string? prediction)
        {
            if (string.IsNullOrWhiteSpace(prediction)) return false;
            var p = prediction.ToLowerInvariant();
            return _highRiskPredictionKeywords.Any(k => p.Contains(k));
        }

        private static string BuildDoctorDisplayName(Doctor doctor)
            => doctor.User != null
                ? $"Dr. {doctor.User.FirstName} {doctor.User.LastName}".Trim()
                : "Your doctor";

        // Surfaces a new ultrasound scan on the patient's Alerts page. A clear/normal
        // result (or a plain upload without AI) is informational; a flagged anomaly is
        // a warning, escalating to danger for high-risk findings.
        private void AddUltrasoundAlert(int patientId, string doctorName, UltrasoundImage record)
        {
            bool anomaly  = IsUltrasoundAnomaly(record.Prediction);
            bool analyzed = !string.IsNullOrWhiteSpace(record.Prediction);

            string title = anomaly ? "Ultrasound Finding Detected" : "New Ultrasound Result";

            string message = anomaly
                ? $"Your recent ultrasound analysis flagged: {record.Prediction}. Please review this with {doctorName}."
                : analyzed
                    ? $"{doctorName} analyzed a new ultrasound scan. Result: {record.Prediction}. View it in your Medical History."
                    : $"{doctorName} uploaded a new ultrasound scan to your records. View it in your Medical History.";

            string severity = anomaly
                ? (IsHighRiskPrediction(record.Prediction) ? AlertTypes.Danger : AlertTypes.Warning)
                : AlertTypes.Info;

            _alertRepository.Add(new Alert
            {
                PatientID   = patientId,
                Title       = title,
                Message     = message,
                AlertType   = severity,
                DateCreated = DateTime.Now,
                IsRead      = false
            });
            _alertRepository.Save();
        }

        // ── GET: Result ────────────────────────────────────────────────────────
        [HttpGet]
        public IActionResult Result(int id)
        {
            var record = _context.UltrasoundImages
                .Include(u => u.Patient).ThenInclude(p => p.User)
                .Include(u => u.Doctor).ThenInclude(d => d.User)
                .FirstOrDefault(u => u.ImageID == id);

            if (record == null) return NotFound();

            var vm = new UltrasoundResultViewModel
            {
                Ultrasound = record,
                ThicknessMm = TryGetThicknessMm(record.AI_Result_JSON),
                PatientName = record.Patient?.User != null
                    ? $"{record.Patient.User.FirstName} {record.Patient.User.LastName}".Trim()
                    : "Patient",
                DoctorName = record.Doctor?.User != null
                    ? $"{record.Doctor.User.FirstName} {record.Doctor.User.LastName}".Trim()
                    : "Doctor"
            };
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int id)
        {
            var record = _ultrasoundRepository.GetById(id);
            if (record == null) return NotFound();

            var accessResult = TryResolveDoctor(record.DoctorID ?? 0, out var doctor);
            if (accessResult != null) return accessResult;

            if (record.DoctorID != doctor!.DoctorID)
            {
                return Forbid();
            }

            var patientId = record.PatientID;
            _ultrasoundRepository.Delete(id);
            _ultrasoundRepository.Save();

            TempData["SuccessMessage"] = "Ultrasound scan deleted successfully.";
            return RedirectToAction(nameof(History), new { id = doctor.DoctorID, patientId });
        }

        private static double? TryGetThicknessMm(string? rawJson)
        {
            if (string.IsNullOrWhiteSpace(rawJson)) return null;

            try
            {
                using var jsonDoc = System.Text.Json.JsonDocument.Parse(rawJson);
                if (jsonDoc.RootElement.TryGetProperty("thickness_mm", out var thicknessElement))
                {
                    if (thicknessElement.TryGetDouble(out var thicknessValue))
                    {
                        return thicknessValue;
                    }

                    if (thicknessElement.ValueKind == System.Text.Json.JsonValueKind.String
                        && double.TryParse(thicknessElement.GetString(), out var thicknessFromString))
                    {
                        return thicknessFromString;
                    }
                }
            }
            catch (System.Text.Json.JsonException)
            {
            }

            return null;
        }

        // ── GET: History ───────────────────────────────────────────────────────
        [HttpGet]
        public IActionResult History(int id, int patientId)
        {
            var accessResult = TryResolveDoctor(id, out var doctor);
            if (accessResult != null) return accessResult;

            var isAssigned = _patientDoctorRepository
                .GetApprovedByDoctor(doctor!.DoctorID)
                .Any(pd => pd.PatientID == patientId);
            if (!isAssigned) return Forbid();

            var patient = _context.Patients
                .Include(p => p.User)
                .FirstOrDefault(p => p.PatientID == patientId);
            if (patient == null) return NotFound();

            var scans = _ultrasoundRepository.GetUltrasoundsByPatientId(patientId)
                .Where(u => u.DoctorID == doctor.DoctorID)
                .ToList();

            ViewData["DoctorId"] = doctor.DoctorID;
            ViewData["SuccessMessage"] = TempData["SuccessMessage"];

            var vm = new UltrasoundHistoryViewModel
            {
                DoctorId = doctor.DoctorID,
                Patient = patient,
                PatientName = patient.User != null
                    ? $"{patient.User.FirstName} {patient.User.LastName}".Trim()
                    : "Patient",
                Scans = scans
            };
            return View(vm);
        }

        // ── GET: Ultrasound Index (Patient Search) ──────────────────────────────
        [HttpGet]
        public IActionResult Index(int? patientId)
        {
            var accessResult = TryResolveDoctor(0, out var doctor);
            if (accessResult != null) return accessResult;

            var patients = _patientDoctorRepository
                .GetApprovedByDoctor(doctor!.DoctorID)
                .Where(pd => pd.Patient != null)
                .Select(pd => pd.Patient!)
                .OrderBy(p => p.User!.LastName)
                .ThenBy(p => p.User!.FirstName)
                .ToList();

            ViewBag.DoctorId = doctor!.DoctorID;
            ViewBag.SelectedPatientId = patientId ?? 0;
            return View("Index", patients);
        }

        // ── API: Search Patients ───────────────────────────────────────────────
        [HttpGet]
        public IActionResult SearchPatients(string q)
        {
            var accessResult = TryResolveDoctor(0, out var doctor, true);
            if (accessResult != null) return accessResult;

            var query = q?.Trim().ToLower() ?? "";

            // Filter before materialising to avoid in-memory table scan
            var patients = _patientDoctorRepository
                .GetApprovedByDoctor(doctor!.DoctorID)
                .Where(pd => pd.Patient != null)
                .Select(pd => pd.Patient!)
                .ToList()  // materialise once; User navigation property needed for name filter
                .Where(p => string.IsNullOrEmpty(query)
                    || $"{p.User?.FirstName} {p.User?.LastName}".Trim().ToLower().Contains(query)
                    || p.PatientID.ToString().Contains(query))
                .OrderBy(p => p.User!.LastName)
                .ThenBy(p => p.User!.FirstName)
                .Take(15)
                .Select(p => new {
                    id = p.PatientID,
                    name = $"{p.User?.FirstName} {p.User?.LastName}".Trim(),
                    email = p.User?.Email
                });

            return Json(patients);
        }

        // ── Helpers ────────────────────────────────────────────────────────────
        private IActionResult? TryResolveDoctor(int id, out Doctor? doctor, bool returnJsonOnFailure = false)
        {
            doctor = null;
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
                return returnJsonOnFailure ? Unauthorized(new { success = false }) : Unauthorized();

            doctor = _context.Doctors.Include(d => d.User).FirstOrDefault(d => d.UserID == userId);
            if (doctor == null)
                return returnJsonOnFailure ? Json(new { success = false }) : NotFound();

            if (id > 0 && doctor.DoctorID != id)
                return returnJsonOnFailure
                    ? StatusCode(StatusCodes.Status403Forbidden, new { success = false })
                    : Forbid();

            return null;
        }
    }

}
