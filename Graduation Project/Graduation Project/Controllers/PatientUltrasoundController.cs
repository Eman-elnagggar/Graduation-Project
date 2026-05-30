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
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Graduation_Project.Controllers
{
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