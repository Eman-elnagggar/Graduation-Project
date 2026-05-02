using Graduation_Project.Interfaces;
using Graduation_Project.Models;
using Graduation_Project.Services;
using Graduation_Project.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Graduation_Project.Controllers
{
    [Authorize(Roles = "Patient")]
    public class PatientMedicationController : Controller
    {
        private readonly IPatient _patientRepository;
        private readonly MedicationService _medicationService;
        private readonly MedicationAdherenceService _adherenceService;
        private readonly MedicationReminderService _reminderService;

        public PatientMedicationController(
            IPatient patientRepository,
            MedicationService medicationService,
            MedicationAdherenceService adherenceService,
            MedicationReminderService reminderService)
        {
            _patientRepository = patientRepository;
            _medicationService = medicationService;
            _adherenceService = adherenceService;
            _reminderService = reminderService;
        }

        public IActionResult Index(int id)
        {
            var (patient, failure) = AuthorizePatientAccess(id);
            if (failure != null)
                return failure;

            var settings = _medicationService.GetOrCreateReminderSettings(id);

            var viewModel = new PatientMedicationIndexViewModel
            {
                Patient = patient,
                UserName = patient.User?.FirstName ?? "Patient",
                ActiveMedications = _medicationService.GetActiveMedications(id).ToList(),
                GlobalLeadTimeMinutes = settings.LeadTimeMinutes
            };

            return View("~/Views/PatientMedication/Index.cshtml", viewModel);
        }

        public IActionResult Daily(int id, DateTime? date)
        {
            var (patient, failure) = AuthorizePatientAccess(id);
            if (failure != null)
                return failure;

            var targetDate = date?.Date ?? DateTime.Today;
            var viewModel = new PatientMedicationDailyViewModel
            {
                Patient = patient,
                UserName = patient.User?.FirstName ?? "Patient",
                Date = targetDate,
                DueSlots = _reminderService.GetDueSlots(id, targetDate)
            };

            return View("~/Views/PatientMedication/Daily.cshtml", viewModel);
        }

        public IActionResult Add(int id)
        {
            var (patient, failure) = AuthorizePatientAccess(id);
            if (failure != null)
                return failure;

            var viewModel = new PatientMedicationAddViewModel
            {
                Patient = patient,
                UserName = patient.User?.FirstName ?? "Patient"
            };

            return View("~/Views/PatientMedication/Add.cshtml", viewModel);
        }

        public class AddMedicationRequest
        {
            public int PatientId { get; set; }
            public string Name { get; set; } = string.Empty;
            public string? Dosage { get; set; }
            public string? Frequency { get; set; }
            public string? Instructions { get; set; }
            public DateTime? StartDate { get; set; }
            public int? DurationDays { get; set; }
        }

        public class LogDoseRequest
        {
            public int PatientId { get; set; }
            public int MedicationId { get; set; }
            public DateTime ScheduledAt { get; set; }
            public string Status { get; set; } = string.Empty;
            public string? Notes { get; set; }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddMedication([FromBody] AddMedicationRequest request)
        {
            var (_, failure) = AuthorizePatientAccess(request.PatientId, true);
            if (failure != null)
                return failure;

            if (string.IsNullOrWhiteSpace(request.Name))
                return Json(new { success = false, message = "Medication name is required." });

            var start = request.StartDate?.Date ?? DateTime.Today;
            _medicationService.AddSelfMedication(
                request.PatientId,
                request.Name,
                request.Dosage ?? string.Empty,
                request.Frequency ?? string.Empty,
                request.Instructions ?? string.Empty,
                start,
                request.DurationDays);

            return Json(new { success = true });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult LogDose([FromBody] LogDoseRequest request)
        {
            var (_, failure) = AuthorizePatientAccess(request.PatientId, true);
            if (failure != null)
                return failure;

            if (!Enum.TryParse<MedicationLogStatus>(request.Status, true, out var parsedStatus))
                parsedStatus = MedicationLogStatus.Scheduled;

            _adherenceService.LogDose(request.MedicationId, request.ScheduledAt, parsedStatus, request.Notes);

            return Json(new { success = true });
        }

        public class ReminderSettingRequest
        {
            public int PatientId { get; set; }
            public int LeadTimeMinutes { get; set; }
        }

        public class MedicationReminderRequest
        {
            public int PatientId { get; set; }
            public int MedicationId { get; set; }
            public int? LeadTimeMinutes { get; set; }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SaveGlobalLeadTime([FromBody] ReminderSettingRequest request)
        {
            var (_, failure) = AuthorizePatientAccess(request.PatientId, true);
            if (failure != null)
                return failure;

            var minutes = Math.Clamp(request.LeadTimeMinutes, 0, 180);
            var settings = _medicationService.GetOrCreateReminderSettings(request.PatientId);
            settings.LeadTimeMinutes = minutes;
            settings.UpdatedAt = DateTime.Now;
            _medicationService.SaveReminderSettings(settings);

            return Json(new { success = true });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SaveMedicationLeadTime([FromBody] MedicationReminderRequest request)
        {
            var (_, failure) = AuthorizePatientAccess(request.PatientId, true);
            if (failure != null)
                return failure;

            var minutes = request.LeadTimeMinutes.HasValue
                ? Math.Clamp(request.LeadTimeMinutes.Value, 0, 180)
                : (int?)null;

            _medicationService.UpdateMedicationLeadTime(request.MedicationId, minutes);
            return Json(new { success = true });
        }

        public class DeleteMedicationRequest
        {
            public int PatientId { get; set; }
            public int MedicationId { get; set; }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteMedication([FromBody] DeleteMedicationRequest request)
        {
            var (_, failure) = AuthorizePatientAccess(request.PatientId, true);
            if (failure != null)
                return failure;

            var ok = _medicationService.RemoveMedicationForPatient(request.MedicationId, request.PatientId);
            if (!ok)
                return Json(new { success = false, message = "Medication not found or access denied." });

            return Json(new { success = true });
        }

        private (Patient? patient, IActionResult? failure) AuthorizePatientAccess(int patientId, bool returnJsonOnFailure = false)
        {
            var patient = _patientRepository.GetById(patientId);
            if (patient == null)
                return (null, NotFound());

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
            {
                if (returnJsonOnFailure)
                    return (null, Unauthorized(new { success = false, message = "Unauthorized." }));

                return (null, Unauthorized());
            }

            if (!string.Equals(patient.UserID, userId, StringComparison.Ordinal))
            {
                if (returnJsonOnFailure)
                    return (null, StatusCode(StatusCodes.Status403Forbidden, new { success = false, message = "Access denied." }));

                return (null, Forbid());
            }

            return (patient, null);
        }
    }
}
