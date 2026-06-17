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
        private readonly IAlert _alertRepository;
        private readonly IPatient _patientRepository;
        private readonly ILogger<UltrasoundController> _logger;

        public UltrasoundController(
            AppDbContext context,
            IPatientDoctor patientDoctorRepository,
            IUltrasoundImage ultrasoundRepository,
            IUltrasoundAIService aiService,
            UltrasoundImageStorage storage,
            IAlert alertRepository,
            IPatient patientRepository,
            ILogger<UltrasoundController> logger)
        {
            _context = context;
            _patientDoctorRepository = patientDoctorRepository;
            _ultrasoundRepository = ultrasoundRepository;
            _aiService = aiService;
            _storage = storage;
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

            return RedirectToAction(nameof(Index), new { patientId });
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

                var alert = new Alert
                {
                    PatientID = model.PatientId,
                    Title = "Ultrasound Analysis Ready",
                    Message = $"{doctorName} uploaded and analyzed an ultrasound scan for you. Result: {prediction}. View the full result in your Medical History.",
                    AlertType = AlertTypes.Info,
                    DateCreated = DateTime.Now,
                    IsRead = false
                };

                _alertRepository.Add(alert);
                _alertRepository.Save();
            }

            return RedirectToAction(nameof(Result), new { id = record.ImageID });
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

            var patients = _patientDoctorRepository
                .GetApprovedByDoctor(doctor!.DoctorID)
                .Where(pd => pd.Patient != null)
                .Select(pd => pd.Patient!)
                .ToList()
                .Where(p => {
                    var name = $"{p.User?.FirstName} {p.User?.LastName}".Trim().ToLower();
                    var id = p.PatientID.ToString();
                    return string.IsNullOrEmpty(query) || name.Contains(query) || id.Contains(query);
                })
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

    // ── PATIENT ULTRASOUND CONTROLLER ─────────────────────────────────────────
    [Authorize(Roles = "Patient")]
    public class PatientUltrasoundController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IPatient _patientRepository;
        private readonly IUltrasoundImage _ultrasoundRepository;
        private readonly UltrasoundImageStorage _storage;

        public PatientUltrasoundController(
            AppDbContext context,
            IPatient patientRepository,
            IUltrasoundImage ultrasoundRepository,
            UltrasoundImageStorage storage)
        {
            _context = context;
            _patientRepository = patientRepository;
            _ultrasoundRepository = ultrasoundRepository;
            _storage = storage;
        }

        [HttpGet]
        public IActionResult History(int id)
        {
            var (patient, failure) = AuthorizePatientAccess(id);
            if (failure != null) return failure;

            var allScans = _ultrasoundRepository.GetUltrasoundsByPatientId(id).ToList();

            var vm = new PatientUltrasoundHistoryViewModel
            {
                Patient = patient!,
                PatientName = patient!.User != null
                    ? $"{patient.User.FirstName} {patient.User.LastName}".Trim()
                    : "Patient",
                DoctorScans = allScans
                    .Where(u => !u.IsPatientUploaded && u.Status == UltrasoundStatus.Completed)
                    .ToList(),
                SelfScans = allScans
                    .Where(u => u.IsPatientUploaded)
                    .ToList()
            };

            return View("~/Views/Patient/UltrasoundHistory.cshtml", vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upload(int id, IFormFile? ImageFile, string? Comment, CancellationToken cancellationToken)
        {
            var (_, failure) = AuthorizePatientAccess(id, true);
            if (failure != null) return failure;

            if (!_storage.IsValid(ImageFile, out var errMsg))
                return BadRequest(new { success = false, message = errMsg });

            var originalPath = await _storage.SaveOriginalAsync(ImageFile!, cancellationToken);

            var record = new UltrasoundImage
            {
                PatientID = id,
                DoctorID = null,
                UploadDate = DateTime.Now,
                OriginalImagePath = originalPath,
                ImagePath = originalPath,
                Status = UltrasoundStatus.Completed,
                AI_Result_JSON = string.Empty,
                DetectedAnomaly = string.Empty,
                DoctorComments = Comment ?? string.Empty,
                Prediction = string.Empty,
                ResultImagePath = string.Empty,
                IsPatientUploaded = true
            };

            _ultrasoundRepository.Add(record);
            _ultrasoundRepository.Save();

            return Json(new
            {
                success = true,
                imageId = record.ImageID,
                imagePath = record.OriginalImagePath,
                uploadDate = record.UploadDate.ToString("MMM dd, yyyy"),
                comment = record.DoctorComments
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteSelfUpload(int id, int imageId)
        {
            var (_, failure) = AuthorizePatientAccess(id, true);
            if (failure != null) return failure;

            var record = _ultrasoundRepository.GetById(imageId);
            if (record == null || record.PatientID != id || !record.IsPatientUploaded)
                return Json(new { success = false, message = "Record not found." });

            _ultrasoundRepository.Delete(imageId);
            _ultrasoundRepository.Save();
            return Json(new { success = true });
        }

        private (Patient? patient, IActionResult? failure) AuthorizePatientAccess(int patientId, bool returnJson = false)
        {
            var patient = _patientRepository.GetById(patientId);
            if (patient == null) return (null, NotFound());

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
                return (null, returnJson ? Unauthorized(new { success = false }) : (IActionResult)Unauthorized());

            if (!string.Equals(patient.UserID, userId, StringComparison.Ordinal))
                return (null, returnJson
                    ? StatusCode(StatusCodes.Status403Forbidden, new { success = false })
                    : (IActionResult)Forbid());

            return (patient, null);
        }
    }
}
