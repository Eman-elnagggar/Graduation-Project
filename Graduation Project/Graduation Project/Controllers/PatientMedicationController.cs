using Graduation_Project.Data;
using Graduation_Project.Interfaces;
using Graduation_Project.Models;
using Graduation_Project.Services;
using Graduation_Project.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text;

namespace Graduation_Project.Controllers
{
    [Authorize(Roles = "Patient")]
    public class PatientMedicationController : Controller
    {
        private readonly IPatient _patientRepository;
        private readonly MedicationService _medicationService;
        private readonly MedicationAdherenceService _adherenceService;
        private readonly MedicationReminderService _reminderService;
        private readonly AppDbContext _context;

        public PatientMedicationController(
            IPatient patientRepository,
            MedicationService medicationService,
            MedicationAdherenceService adherenceService,
            MedicationReminderService reminderService,
            AppDbContext context)
        {
            _patientRepository = patientRepository;
            _medicationService = medicationService;
            _adherenceService = adherenceService;
            _reminderService = reminderService;
            _context = context;
        }

        public IActionResult Index(int id)
        {
            var (patient, failure) = AuthorizePatientAccess(id);
            if (failure != null)
                return failure;

            var settings = _medicationService.GetOrCreateReminderSettings(id);
            var todaySlots = _reminderService.GetDueSlots(id, DateTime.Today)
                .Where(s => s.Status == MedicationLogStatus.Scheduled)
                .ToList();

            var viewModel = new PatientMedicationIndexViewModel
            {
                Patient = patient,
                UserName = patient.User?.FirstName ?? "Patient",
                ActiveMedications = _medicationService.GetActiveMedications(id).ToList(),
                GlobalLeadTimeMinutes = settings.LeadTimeMinutes,
                TodaySlots = todaySlots,
                WeeklyChartData = _adherenceService.GetWeeklyChartData(id)
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

        public IActionResult Edit(int id, int medicationId)
        {
            var (patient, failure) = AuthorizePatientAccess(id);
            if (failure != null)
                return failure;

            var medication = _context.Medications
                .FirstOrDefault(m => m.MedicationId == medicationId && m.PatientID == id);

            if (medication == null)
                return NotFound();

            var viewModel = new PatientMedicationEditViewModel
            {
                Patient = patient,
                UserName = patient.User?.FirstName ?? "Patient",
                Medication = medication
            };

            return View("~/Views/PatientMedication/Edit.cshtml", viewModel);
        }

        public IActionResult History(int id, int medicationId)
        {
            var (patient, failure) = AuthorizePatientAccess(id);
            if (failure != null)
                return failure;

            var medication = _context.Medications
                .Include(m => m.Schedules)
                .FirstOrDefault(m => m.MedicationId == medicationId && m.PatientID == id);

            if (medication == null)
                return NotFound();

            var logs = _context.MedicationLogs
                .Where(l => l.MedicationId == medicationId)
                .OrderByDescending(l => l.ScheduledAt)
                .ToList();

            var summary = logs.Count > 0
                ? _adherenceService.GetSummary(id, logs.Min(l => l.ScheduledAt.Date), DateTime.Today)
                : new MedicationAdherenceSummary { PatientId = id };

            // Narrow summary to this specific medication
            var total = logs.Count;
            var taken = logs.Count(l => l.Status == MedicationLogStatus.Taken);
            var missed = logs.Count(l => l.Status == MedicationLogStatus.Missed);
            var skipped = logs.Count(l => l.Status == MedicationLogStatus.Skipped);
            var medSummary = new MedicationAdherenceSummary
            {
                PatientId = id,
                StartDate = logs.Count > 0 ? logs.Min(l => l.ScheduledAt.Date) : DateTime.Today,
                EndDate = DateTime.Today,
                TotalDoses = total,
                TakenDoses = taken,
                MissedDoses = missed,
                SkippedDoses = skipped,
                AdherencePercent = total > 0 ? Math.Round((double)taken / total * 100, 1) : 0
            };

            var viewModel = new PatientMedicationHistoryViewModel
            {
                Patient = patient,
                UserName = patient.User?.FirstName ?? "Patient",
                Medication = medication,
                Logs = logs,
                Summary = medSummary
            };

            return View("~/Views/PatientMedication/History.cshtml", viewModel);
        }

        public IActionResult ExportCsv(int id)
        {
            var (patient, failure) = AuthorizePatientAccess(id);
            if (failure != null)
                return failure;

            var logs = _context.MedicationLogs
                .Include(l => l.Medication)
                .Where(l => l.Medication.PatientID == id)
                .OrderByDescending(l => l.ScheduledAt)
                .ToList();

            var sb = new StringBuilder();
            sb.AppendLine("Medication,Dosage,Scheduled At,Status,Taken At,Notes");
            foreach (var log in logs)
            {
                var name = $"\"{log.Medication?.Name ?? ""}\"";
                var dosage = $"\"{log.Medication?.Dosage ?? ""}\"";
                var scheduled = log.ScheduledAt.ToString("yyyy-MM-dd HH:mm");
                var status = log.Status.ToString();
                var takenAt = log.TakenAt?.ToString("yyyy-MM-dd HH:mm") ?? "";
                var notes = $"\"{(log.Notes ?? "").Replace("\"", "\"\"")}\"";
                sb.AppendLine($"{name},{dosage},{scheduled},{status},{takenAt},{notes}");
            }

            var bytes = Encoding.UTF8.GetBytes(sb.ToString());
            var fileName = $"medication-history-{DateTime.Today:yyyy-MM-dd}.csv";
            return File(bytes, "text/csv", fileName);
        }

        // ── DTOs ──────────────────────────────────────────────────────────────

        public class AddMedicationRequest
        {
            public int PatientId { get; set; }
            public string Name { get; set; } = string.Empty;
            public string? Dosage { get; set; }
            public string? Frequency { get; set; }
            public string? Instructions { get; set; }
            public DateTime? StartDate { get; set; }
            public int? DurationDays { get; set; }
            public int? TotalPills { get; set; }
            public int? PillsPerDose { get; set; }
        }

        public class UpdateMedicationRequest
        {
            public int PatientId { get; set; }
            public int MedicationId { get; set; }
            public string Name { get; set; } = string.Empty;
            public string? Dosage { get; set; }
            public string? Frequency { get; set; }
            public string? Instructions { get; set; }
            public DateTime? StartDate { get; set; }
            public int? DurationDays { get; set; }
            public int? TotalPills { get; set; }
            public int? PillsPerDose { get; set; }
        }

        public class LogDoseRequest
        {
            public int PatientId { get; set; }
            public int MedicationId { get; set; }
            public DateTime ScheduledAt { get; set; }
            public string Status { get; set; } = string.Empty;
            public string? Notes { get; set; }
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

        public class DeleteMedicationRequest
        {
            public int PatientId { get; set; }
            public int MedicationId { get; set; }
        }

        // ── POST actions ──────────────────────────────────────────────────────

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
            var med = _medicationService.AddSelfMedication(
                request.PatientId,
                request.Name,
                request.Dosage ?? string.Empty,
                request.Frequency ?? string.Empty,
                request.Instructions ?? string.Empty,
                start,
                request.DurationDays);

            if (request.TotalPills.HasValue || request.PillsPerDose.HasValue)
            {
                med.TotalPills = request.TotalPills;
                med.PillsPerDose = request.PillsPerDose;
                _context.SaveChanges();
            }

            return Json(new { success = true });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UpdateMedication([FromBody] UpdateMedicationRequest request)
        {
            var (_, failure) = AuthorizePatientAccess(request.PatientId, true);
            if (failure != null)
                return failure;

            if (string.IsNullOrWhiteSpace(request.Name))
                return Json(new { success = false, message = "Medication name is required." });

            var start = request.StartDate?.Date ?? DateTime.Today;
            var ok = _medicationService.UpdateSelfMedication(
                request.MedicationId,
                request.PatientId,
                request.Name,
                request.Dosage ?? string.Empty,
                request.Frequency ?? string.Empty,
                request.Instructions ?? string.Empty,
                start,
                request.DurationDays,
                request.TotalPills,
                request.PillsPerDose);

            if (!ok)
                return Json(new { success = false, message = "Medication not found or access denied." });

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

        // ── helpers ───────────────────────────────────────────────────────────

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
