using Graduation_Project.Data;
using Graduation_Project.Hubs;
using Graduation_Project.Interfaces;
using Graduation_Project.Models;
using Graduation_Project.Services;
using Graduation_Project.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
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
        private readonly IHubContext<MedicationHub> _medicationHub;

        public PatientMedicationController(
            IPatient patientRepository,
            MedicationService medicationService,
            MedicationAdherenceService adherenceService,
            MedicationReminderService reminderService,
            AppDbContext context,
            IHubContext<MedicationHub> medicationHub)
        {
            _patientRepository = patientRepository;
            _medicationService = medicationService;
            _adherenceService = adherenceService;
            _reminderService = reminderService;
            _context = context;
            _medicationHub = medicationHub;
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
                .Include(m => m.Schedules)
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

        public class MedicationFormRequest
        {
            public int PatientId { get; set; }
            public string? Name { get; set; }
            public string? Dosage { get; set; }
            public string? Form { get; set; }
            public string? FrequencyCode { get; set; }
            public string? FrequencyLabel { get; set; }
            public int? TimesPerDay { get; set; }
            public int? IntervalDays { get; set; }
            public List<string>? Times { get; set; }
            public string? Instructions { get; set; }
            public DateTime? StartDate { get; set; }
            public int? DurationDays { get; set; }
            public int? TotalPills { get; set; }
            public int? PillsPerDose { get; set; }
        }

        public class AddMedicationRequest : MedicationFormRequest
        {
        }

        public class UpdateMedicationRequest : MedicationFormRequest
        {
            public int MedicationId { get; set; }
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
        public IActionResult AddMedication([FromBody] AddMedicationRequest? request)
        {
            if (request == null)
                return Json(new { success = false, message = "Could not read the medication details. Please try again." });

            var (_, failure) = AuthorizePatientAccess(request.PatientId, true);
            if (failure != null)
                return failure;

            if (!TryBuildMedicationInput(request, out var input, out var error))
                return Json(new { success = false, message = error });

            _medicationService.AddSelfMedication(
                request.PatientId,
                input.Name,
                input.Dosage,
                input.Form,
                input.Frequency,
                input.Instructions,
                input.StartDate,
                input.DurationDays,
                input.TotalPills,
                input.PillsPerDose);

            return Json(new { success = true });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UpdateMedication([FromBody] UpdateMedicationRequest? request)
        {
            if (request == null)
                return Json(new { success = false, message = "Could not read the medication details. Please try again." });

            var (_, failure) = AuthorizePatientAccess(request.PatientId, true);
            if (failure != null)
                return failure;

            if (!TryBuildMedicationInput(request, out var input, out var error))
                return Json(new { success = false, message = error });

            var ok = _medicationService.UpdateSelfMedication(
                request.MedicationId,
                request.PatientId,
                input.Name,
                input.Dosage,
                input.Form,
                input.Frequency,
                input.Instructions,
                input.StartDate,
                input.DurationDays,
                input.TotalPills,
                input.PillsPerDose);

            if (!ok)
                return Json(new { success = false, message = "Medication not found or access denied." });

            return Json(new { success = true });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LogDose([FromBody] LogDoseRequest? request)
        {
            if (request == null)
                return Json(new { success = false, message = "Could not read the dose details. Please try again." });

            var (patient, failure) = AuthorizePatientAccess(request.PatientId, true);
            if (failure != null)
                return failure;

            if (!Enum.TryParse<MedicationLogStatus>(request.Status, true, out var parsedStatus))
                parsedStatus = MedicationLogStatus.Scheduled;

            _adherenceService.LogDose(request.MedicationId, request.ScheduledAt, parsedStatus, request.Notes);

            // Broadcast the change to every open tab/device for this patient so the
            // tracker updates live without a page refresh.
            if (!string.IsNullOrEmpty(patient?.UserID))
            {
                await _medicationHub.Clients.User(patient.UserID).SendAsync("DoseUpdated", new
                {
                    medicationId = request.MedicationId,
                    scheduledAt = request.ScheduledAt.ToString("O"),
                    status = parsedStatus.ToString()
                });
            }

            return Json(new { success = true });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SaveGlobalLeadTime([FromBody] ReminderSettingRequest? request)
        {
            if (request == null)
                return Json(new { success = false, message = "Enter a reminder lead time in minutes." });

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
        public IActionResult SaveMedicationLeadTime([FromBody] MedicationReminderRequest? request)
        {
            if (request == null)
                return Json(new { success = false, message = "Enter a reminder lead time in minutes, or leave it blank to use the global setting." });

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
        public IActionResult DeleteMedication([FromBody] DeleteMedicationRequest? request)
        {
            if (request == null)
                return Json(new { success = false, message = "Could not read the request. Please try again." });

            var (_, failure) = AuthorizePatientAccess(request.PatientId, true);
            if (failure != null)
                return failure;

            var ok = _medicationService.RemoveMedicationForPatient(request.MedicationId, request.PatientId);
            if (!ok)
                return Json(new { success = false, message = "Medication not found or access denied." });

            return Json(new { success = true });
        }

        // ── helpers ───────────────────────────────────────────────────────────

        private record MedicationInput(
            string Name,
            string Dosage,
            string? Form,
            MedicationFrequencySpec Frequency,
            string Instructions,
            DateTime StartDate,
            int? DurationDays,
            int? TotalPills,
            int? PillsPerDose);

        /// <summary>
        /// Validates the Add/Edit wizard payload. Only the name and a schedulable
        /// frequency are required — every other field may legitimately be left blank
        /// and is normalised to null rather than rejected.
        /// </summary>
        private static bool TryBuildMedicationInput(
            MedicationFormRequest request,
            out MedicationInput input,
            out string error)
        {
            input = null!;
            error = string.Empty;

            var name = (request.Name ?? string.Empty).Trim();
            if (name.Length == 0)
            {
                error = "Medication name is required.";
                return false;
            }
            if (name.Length > 120)
            {
                error = "Medication name is too long (120 characters max).";
                return false;
            }

            if (request.DurationDays is < 0)
            {
                error = "Duration cannot be negative.";
                return false;
            }
            if (request.DurationDays is > 3650)
            {
                error = "Duration must be 3650 days or fewer.";
                return false;
            }
            if (request.TotalPills is < 0)
            {
                error = "Total pills cannot be negative.";
                return false;
            }
            if (request.PillsPerDose is < 1)
            {
                error = "Pills per dose must be at least 1.";
                return false;
            }

            var times = new List<TimeSpan>();
            foreach (var raw in request.Times ?? Enumerable.Empty<string>())
            {
                if (string.IsNullOrWhiteSpace(raw))
                    continue;

                if (!TimeSpan.TryParse(raw.Trim(), out var parsed) || parsed < TimeSpan.Zero || parsed >= TimeSpan.FromDays(1))
                {
                    error = $"\"{raw}\" is not a valid dose time.";
                    return false;
                }

                times.Add(parsed);
            }

            var spec = MedicationFrequencies.Build(
                request.FrequencyCode,
                request.TimesPerDay,
                request.IntervalDays,
                times);

            if (!string.IsNullOrWhiteSpace(request.FrequencyLabel))
                spec.Label = request.FrequencyLabel.Trim();

            input = new MedicationInput(
                name,
                (request.Dosage ?? string.Empty).Trim(),
                string.IsNullOrWhiteSpace(request.Form) ? null : request.Form.Trim(),
                spec,
                (request.Instructions ?? string.Empty).Trim(),
                request.StartDate?.Date ?? DateTime.Today,
                request.DurationDays > 0 ? request.DurationDays : null,
                request.TotalPills > 0 ? request.TotalPills : null,
                request.PillsPerDose);

            return true;
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
