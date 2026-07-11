using Graduation_Project.Data;
using Graduation_Project.Helpers;
using Graduation_Project.Interfaces;
using Graduation_Project.Models;
using Graduation_Project.Services;
using Graduation_Project.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace Graduation_Project.Controllers
{
    [Authorize(Roles = "Assistant")]
    public class AssistantController : Controller
    {
        private readonly IAssistant _assistantRepository;
        private readonly IClinic _clinicRepository;
        private readonly IAppointment _appointmentRepository;
        private readonly IBooking _bookingRepository;
        private readonly IPatientDoctor _patientDoctorRepository;
        private readonly IAlert _alertRepository;
        private readonly ILabTest _labTestRepository;
        private readonly AssistantScheduleService _assistantScheduleService;
        private readonly IDoctorNotificationService _doctorNotificationService;
        private readonly IPatientNotificationService _patientNotificationService;
        private readonly AppDbContext _context;
        private readonly IChatMessageCrypto _chatMessageCrypto;
        private readonly IWebHostEnvironment _env;
        private readonly IEmailService _emailService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public AssistantController(
            IAssistant assistantRepository,
            IClinic clinicRepository,
            IAppointment appointmentRepository,
            IBooking bookingRepository,
            IPatientDoctor patientDoctorRepository,
            IAlert alertRepository,
            ILabTest labTestRepository,
            AssistantScheduleService assistantScheduleService,
            IDoctorNotificationService doctorNotificationService,
            IPatientNotificationService patientNotificationService,
            AppDbContext context,
            IChatMessageCrypto chatMessageCrypto,
            IWebHostEnvironment env,
            IEmailService emailService,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            _assistantRepository = assistantRepository;
            _clinicRepository = clinicRepository;
            _appointmentRepository = appointmentRepository;
            _bookingRepository = bookingRepository;
            _patientDoctorRepository = patientDoctorRepository;
            _alertRepository = alertRepository;
            _labTestRepository = labTestRepository;
            _assistantScheduleService = assistantScheduleService;
            _doctorNotificationService = doctorNotificationService;
            _patientNotificationService = patientNotificationService;
            _context = context;
            _chatMessageCrypto = chatMessageCrypto;
            _env = env;
            _emailService = emailService;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        // ── Clinic Guard ────────────────────────────────────────────────────────
        // If the assistant has no clinic yet, only the invitation-related actions
        // are allowed. Every other page is redirected to ClinicInvitations.
        private static readonly HashSet<string> _allowedWithoutClinic =
            new(StringComparer.OrdinalIgnoreCase)
            {
                nameof(ClinicInvitations),
                nameof(AcceptClinicInvitation),
                nameof(DeclineClinicInvitation)
            };

        public override void OnActionExecuting(Microsoft.AspNetCore.Mvc.Filters.ActionExecutingContext context)
        {
            base.OnActionExecuting(context);

            var actionName = context.ActionDescriptor.RouteValues["action"] ?? string.Empty;
            if (_allowedWithoutClinic.Contains(actionName))
                return;

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
                return; // let [Authorize] handle unauthenticated users

            var assistant = _context.Assistants
                .AsNoTracking()
                .FirstOrDefault(a => a.UserID == userId);

            if (assistant != null && assistant.ClinicID == null)
            {
                context.Result = RedirectToAction(
                    nameof(ClinicInvitations),
                    new { id = assistant.AssistantID });
            }
        }
        // ────────────────────────────────────────────────────────────────────────

        public IActionResult Index(int id, int? doctorId, DateTime? date, string? status)
        {
            // OnActionExecuting guarantees ClinicID != null by this point
            var accessResult = TryResolveAssistantClinic(id, out var assistant, out var clinic);
            if (accessResult != null) return accessResult;

            var selectedDate = date?.Date ?? DateTime.Today;
            var selectedStatus = NormalizeScheduleStatus(status);

            var relevantDoctorIds = GetRelevantDoctorIds(assistant, clinic);
            var doctorSummaries = BuildDoctorSummaries(assistant, clinic, relevantDoctorIds);

            bool isFiltered = doctorId.HasValue && relevantDoctorIds.Contains(doctorId.Value);
            var selectedDoctorName = isFiltered
                ? doctorSummaries.FirstOrDefault(d => d.DoctorID == doctorId.Value)?.FullName ?? "Doctor"
                : "All Doctors";

            // Return page skeleton — heavy data (stats, schedule) loaded via AJAX
            var patientIds = _patientDoctorRepository
                .GetApprovedByDoctors(relevantDoctorIds)
                .Select(pd => pd.PatientID)
                .Distinct()
                .ToList();

            var recentAlerts = _patientNotificationService
                .GetForPatients(patientIds, PatientNotificationTypes.Operational)
                .Where(n => !n.IsRead)
                .Take(5)
                .ToList();

            var viewModel = new AssistantDashboardViewModel
            {
                Assistant = assistant,
                AssistantName = BuildAssistantDisplayName(assistant.User),
                Clinic = clinic,
                ClinicName = clinic.Name ?? "Clinic",
                SelectedDate = selectedDate,
                SelectedScheduleStatus = selectedStatus,
                Doctors = doctorSummaries,
                SelectedDoctorID = isFiltered ? doctorId : null,
                SelectedDoctorName = selectedDoctorName,
                RecentAlerts = recentAlerts
            };

            return View(viewModel);
        }

        public IActionResult Messages(int id)
        {
            var accessResult = TryResolveAssistantClinic(id, out var assistant, out var clinic);
            if (accessResult != null) return accessResult;

            var relevantDoctorIds = GetRelevantDoctorIds(assistant, clinic);
            var approvedLinks = _patientDoctorRepository
                .GetApprovedByDoctors(relevantDoctorIds)
                .Where(pd => pd.Patient != null && !string.IsNullOrWhiteSpace(pd.Patient.UserID))
                .GroupBy(pd => pd.PatientID)
                .Select(g => g.First())
                .ToList();

            var linkedDoctors = _context.Doctors
                .AsNoTracking()
                .Include(d => d.User)
                .Where(d => relevantDoctorIds.Contains(d.DoctorID)
                         && !string.IsNullOrWhiteSpace(d.UserID))
                .ToList();

            var assistantUserId = assistant.UserID;
            var patientUserIds = approvedLinks
                .Select(pd => pd.Patient!.UserID)
                .Where(userId => !string.IsNullOrWhiteSpace(userId))
                .Distinct()
                .ToList();

            var doctorUserIds = linkedDoctors
                .Select(d => d.UserID)
                .Where(userId => !string.IsNullOrWhiteSpace(userId))
                .Distinct()
                .ToList();

            var receiverUserIds = patientUserIds
                .Concat(doctorUserIds)
                .Distinct()
                .ToList();

            var chatMessages = _context.ChatMessages
                .Where(m => (m.SenderUserId == assistantUserId && receiverUserIds.Contains(m.ReceiverUserId))
                         || (m.ReceiverUserId == assistantUserId && receiverUserIds.Contains(m.SenderUserId)))
                .OrderByDescending(m => m.SentAtUtc)
                .ToList();

            var patientConversations = approvedLinks
                .Select(pd => new
                {
                    participantId = pd.PatientID,
                    participantType = "Patient",
                    ReceiverUserId = pd.Patient?.UserID ?? string.Empty,
                    participantName = pd.Patient?.User != null
                        ? $"{pd.Patient.User.FirstName} {pd.Patient.User.LastName}".Trim()
                        : "Patient",
                });

            var doctorConversations = linkedDoctors
                .Select(d => new
                {
                    participantId = d.DoctorID,
                    participantType = "Doctor",
                    ReceiverUserId = d.UserID,
                    participantName = d.User != null
                        ? $"Dr. {d.User.FirstName} {d.User.LastName}".Trim()
                        : "Doctor"
                });

            var conversations = patientConversations
                .Concat(doctorConversations)
                .Where(c => !string.IsNullOrWhiteSpace(c.ReceiverUserId))
                .GroupBy(c => c.ReceiverUserId)
                .Select(g => g.First())
                .Select(c => new AssistantConversationSummary
                {
                    ParticipantId = c.participantId,
                    ParticipantType = c.participantType,
                    ReceiverUserId = c.ReceiverUserId,
                    ParticipantName = c.participantName,
                    UnreadCount = chatMessages.Count(m => m.SenderUserId == c.ReceiverUserId && m.ReceiverUserId == assistantUserId && !m.IsRead),
                    LastMessageTime = chatMessages
                        .Where(m => m.SenderUserId == c.ReceiverUserId || m.ReceiverUserId == c.ReceiverUserId)
                        .Select(m => (DateTime?)m.SentAtUtc)
                        .FirstOrDefault(),
                    LastMessagePreview = chatMessages
                        .Where(m => m.SenderUserId == c.ReceiverUserId || m.ReceiverUserId == c.ReceiverUserId)
                        .Select(m => _chatMessageCrypto.Decrypt(m.Message))
                        .FirstOrDefault() ?? "Start a conversation"
                })
                .OrderBy(c => c.ParticipantType)
                .ThenBy(c => c.ParticipantName)
                .ToList();

            var vm = new AssistantMessagesViewModel
            {
                Assistant = assistant,
                AssistantName = BuildAssistantDisplayName(assistant.User),
                Conversations = conversations
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadChatFile(int id, IFormFile file)
        {
            var accessResult = TryResolveAssistantClinic(id, out var assistant, out _);
            if (accessResult != null) return accessResult;

            if (file == null || file.Length == 0)
                return BadRequest(new { error = "No file provided." });

            if (file.Length > 10 * 1024 * 1024)
                return BadRequest(new { error = "File exceeds the 10 MB limit." });

            var allowedTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "image/jpeg", "image/png", "image/gif", "image/webp",
                "application/pdf"
            };

            if (!allowedTypes.Contains(file.ContentType))
                return BadRequest(new { error = "File type not allowed." });

            var userDir = Path.Combine(_env.WebRootPath, "uploads", "chat", assistant!.UserID!);
            Directory.CreateDirectory(userDir);

            var ext = Path.GetExtension(file.FileName);
            var fileName = $"{Guid.NewGuid():N}{ext}";
            var filePath = Path.Combine(userDir, fileName);

            using (var stream = System.IO.File.Create(filePath))
                await file.CopyToAsync(stream);

            var url = $"/uploads/chat/{assistant.UserID}/{fileName}";
            var type = file.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase) ? "image" : "file";

            return Json(new { url, type, name = file.FileName });
        }

        [HttpGet]
        public IActionResult ConversationMessages(int id, string userId)
        {
            var accessResult = TryResolveAssistantClinic(id, out var assistant, out var clinic);
            if (accessResult != null) return accessResult;

            var relevantDoctorIds = GetRelevantDoctorIds(assistant, clinic);
            var linkedPatientUserIds = _patientDoctorRepository
                .GetApprovedByDoctors(relevantDoctorIds)
                .Select(pd => pd.Patient)
                .Where(p => p != null && !string.IsNullOrWhiteSpace(p.UserID))
                .Select(p => p!.UserID!)
                .Distinct()
                .ToList();

            var linkedDoctorUserIds = _context.Doctors
                .AsNoTracking()
                .Where(d => relevantDoctorIds.Contains(d.DoctorID)
                         && !string.IsNullOrWhiteSpace(d.UserID))
                .Select(d => d.UserID!)
                .ToList();

            var linkedUserIds = linkedPatientUserIds
                .Concat(linkedDoctorUserIds)
                .Distinct()
                .ToList();

            if (string.IsNullOrWhiteSpace(userId) || !linkedUserIds.Contains(userId))
                return Forbid();

            if (string.IsNullOrWhiteSpace(assistant.UserID))
                return NotFound();

            var assistantUserId = assistant.UserID;
            var receiverUserId = userId;

            var messages = _context.ChatMessages
                .Where(m => (m.SenderUserId == assistantUserId && m.ReceiverUserId == receiverUserId)
                         || (m.SenderUserId == receiverUserId && m.ReceiverUserId == assistantUserId))
                .OrderBy(m => m.SentAtUtc)
                .ToList()
                .Select(m => new
                {
                    id = m.ChatMessageId,
                    senderId = m.SenderUserId,
                    receiverId = m.ReceiverUserId,
                    content = _chatMessageCrypto.Decrypt(m.Message),
                    timestamp = m.SentAtUtc.AsUtcOffset(),
                    attachmentUrl = m.AttachmentUrl,
                    attachmentType = m.AttachmentType,
                    attachmentName = m.AttachmentName
                })
                .ToList();

            var unreadIncoming = _context.ChatMessages
                .Where(m => m.SenderUserId == receiverUserId
                         && m.ReceiverUserId == assistantUserId
                         && !m.IsRead)
                .ToList();

            if (unreadIncoming.Count > 0)
            {
                var now = DateTime.UtcNow;
                foreach (var msg in unreadIncoming)
                {
                    msg.IsRead = true;
                    msg.ReadAtUtc = now;
                }

                _context.SaveChanges();
            }

            return Json(messages);
        }

        [HttpGet]
        public IActionResult GetDashboardStats(int id, int? doctorId, string? date)
        {
            var accessResult = TryResolveAssistantClinic(id, out var assistant, out var clinic, true);
            if (accessResult != null) return accessResult;

            var targetDate = ParseDashboardDate(date);
            var relevantDoctorIds = GetRelevantDoctorIds(assistant, clinic);
            bool isFiltered = doctorId.HasValue && relevantDoctorIds.Contains(doctorId.Value);
            var activeDoctorIds = isFiltered ? new List<int> { doctorId.Value } : relevantDoctorIds;

            var weekStart = targetDate.AddDays(-(int)targetDate.DayOfWeek);

            var allTodaysAppointments = _appointmentRepository
                .GetByClinicAndDate(clinic.ClinicID, targetDate)
                .Where(a => a.isBooked)
                .ToList();
            var allApprovedPatientDoctors = _patientDoctorRepository
                .GetApprovedByDoctors(relevantDoctorIds).ToList();

            // Pre-aggregate counts per doctor (booked only)
            var appointmentCountsByDoctor = allTodaysAppointments
                .GroupBy(a => a.DoctorID)
                .ToDictionary(g => g.Key, g => g.Count());
            var patientCountsByDoctor = allApprovedPatientDoctors
                .GroupBy(pd => pd.DoctorID)
                .ToDictionary(g => g.Key, g => g.Count());

            // Filtered counts (booked only)
            var filteredAppointmentCount = isFiltered
                ? allTodaysAppointments.Count(a => a.DoctorID == doctorId!.Value)
                : allTodaysAppointments.Count;

            var filteredPatientDoctors = isFiltered
                ? allApprovedPatientDoctors.Where(pd => pd.DoctorID == doctorId!.Value)
                : allApprovedPatientDoctors;
            var uniquePatientIds = filteredPatientDoctors
                .Select(pd => pd.PatientID).Distinct().ToList();

            var pendingAlertsCount = uniquePatientIds.Any()
                ? _context.PatientNotifications
                    .AsNoTracking()
                    .Count(n => uniquePatientIds.Contains(n.PatientID) && !n.IsRead && n.NotificationType == PatientNotificationTypes.Operational)
                : 0;

            var testsThisWeek = isFiltered
                ? _labTestRepository.CountByDoctorSince(doctorId!.Value, weekStart)
                : _labTestRepository.CountByDoctorsSince(activeDoctorIds, weekStart);

            return Json(new
            {
                selectedDate = targetDate.ToString("yyyy-MM-dd"),
                selectedDateLabel = targetDate.ToString("dddd, MMM dd, yyyy"),
                todayAppointmentsCount = filteredAppointmentCount,
                totalPatients = uniquePatientIds.Count,
                pendingAlertsCount,
                testsThisWeek,
                doctorCounts = relevantDoctorIds.Select(dId => new
                {
                    doctorId = dId,
                    todayAppointments = appointmentCountsByDoctor.GetValueOrDefault(dId),
                    totalPatients = patientCountsByDoctor.GetValueOrDefault(dId)
                }).ToList()
            });
        }

        [HttpGet]
        public IActionResult GetUnreadAlertsCount(int id)
        {
            var accessResult = TryResolveAssistantClinic(id, out var assistant, out var clinic, true);
            if (accessResult != null) return accessResult;

            var relevantDoctorIds = GetRelevantDoctorIds(assistant, clinic);
            var patientIds = _patientDoctorRepository
                .GetApprovedByDoctors(relevantDoctorIds)
                .Select(pd => pd.PatientID)
                .Distinct()
                .ToList();

            var unreadCount = patientIds.Any()
                ? _context.PatientNotifications
                    .AsNoTracking()
                    .Count(n => patientIds.Contains(n.PatientID) && !n.IsRead && n.NotificationType == PatientNotificationTypes.Operational)
                : 0;

            return Json(new { unreadCount });
        }

        [HttpGet]
        public IActionResult GetNotificationsJson(int id)
        {
            var accessResult = TryResolveAssistantClinic(id, out var assistant, out var clinic, true);
            if (accessResult != null) return accessResult;

            var relevantDoctorIds = GetRelevantDoctorIds(assistant, clinic);
            var patientIds = _patientDoctorRepository
                .GetApprovedByDoctors(relevantDoctorIds)
                .Select(pd => pd.PatientID)
                .Distinct()
                .ToList();

            var alerts = patientIds.Any()
                ? _context.PatientNotifications
                    .AsNoTracking()
                    .Include(n => n.Patient)
                        .ThenInclude(p => p.User)
                    .Where(n => patientIds.Contains(n.PatientID) && n.NotificationType == PatientNotificationTypes.Operational)
                    .OrderByDescending(n => n.DateCreated)
                    .Take(20)
                    .ToList()
                : new List<PatientNotification>();

            var result = alerts.Select(n => new
            {
                alertId   = n.Id,
                title     = n.Title,
                message   = n.Message,
                alertType = n.Severity ?? "info",
                dateCreated = n.DateCreated.ToString("o"),
                isRead    = n.IsRead,
                patientName = n.Patient?.User != null
                    ? $"{n.Patient.User.FirstName} {n.Patient.User.LastName}".Trim()
                    : "Patient"
            });

            return Json(result);
        }

        [HttpGet]
        public IActionResult GetScheduleByDate(int id, int? doctorId, string? date, string? status)
        {
            var accessResult = TryResolveAssistantClinic(id, out var assistant, out var clinic, true);
            if (accessResult != null) return accessResult;

            var targetDate = ParseDashboardDate(date);
            var selectedStatus = NormalizeScheduleStatus(status);
            var relevantDoctorIds = GetRelevantDoctorIds(assistant, clinic);
            bool isFiltered = doctorId.HasValue && relevantDoctorIds.Contains(doctorId.Value);

            var allTodaysAppointments = _appointmentRepository
                .GetByClinicAndDate(clinic.ClinicID, targetDate).ToList();

            var scopeAppointments = isFiltered
                ? allTodaysAppointments.Where(a => a.DoctorID == doctorId!.Value)
                : allTodaysAppointments.AsEnumerable();

            var filteredAppointments = selectedStatus switch
            {
                "Cancelled" => scopeAppointments
                    .Where(a => string.Equals(a.Booking?.Status, "Cancelled", StringComparison.OrdinalIgnoreCase))
                    .ToList(),
                _ => scopeAppointments
                    .Where(a => a.isBooked)
                    .ToList()
            };

            ViewBag.ClinicName = clinic.Name ?? "Clinic";
            ViewBag.SelectedDoctorID = isFiltered ? doctorId : null;
            ViewBag.SelectedDoctorName = isFiltered
                ? BuildDoctorSummaries(assistant, clinic, relevantDoctorIds)
                    .FirstOrDefault(d => d.DoctorID == doctorId!.Value)?.FullName ?? "Doctor"
                : "All Doctors";
            ViewBag.SelectedDateLabel = targetDate.ToString("MMM dd, yyyy");
            ViewBag.SelectedStatusLabel = selectedStatus;
            ViewBag.HasDoctors = relevantDoctorIds.Any();

            return PartialView("_TodaysSchedule", filteredAppointments);
        }

        [HttpGet]
        public IActionResult GetTodaysSchedule(int id, int? doctorId, string? date, string? status)
            => GetScheduleByDate(id, doctorId, date, status);

        private static DateTime ParseDashboardDate(string? date)
        {
            if (DateTime.TryParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
                return parsed.Date;

            return DateTime.Today;
        }

        private static string BuildAssistantDisplayName(ApplicationUser? user)
        {
            if (user == null)
                return "Assistant";

            var firstName = user.FirstName?.Trim();
            var lastName = user.LastName?.Trim();

            var fullName = string.Join(" ", new[] { firstName, lastName }
                .Where(s => !string.IsNullOrWhiteSpace(s))).Trim();

            if (!string.IsNullOrWhiteSpace(fullName))
                return fullName;

            var fallback = user.UserName?.Trim();
            if (string.IsNullOrWhiteSpace(fallback))
                fallback = user.Email?.Trim();

            return NormalizeDisplayName(fallback, "Assistant");
        }

        private static string NormalizeDisplayName(string? input, string defaultName)
        {
            if (string.IsNullOrWhiteSpace(input))
                return defaultName;

            var value = input.Trim();
            if (value.Contains('@'))
            {
                value = value.Split('@', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? defaultName;
            }

            value = value.Replace('.', ' ').Replace('_', ' ').Replace('-', ' ').Trim();
            if (string.IsNullOrWhiteSpace(value))
                return defaultName;

            return CultureInfo.InvariantCulture.TextInfo.ToTitleCase(value.ToLowerInvariant());
        }

        private static string NormalizeScheduleStatus(string? status)
        {
            if (string.Equals(status, "cancelled", StringComparison.OrdinalIgnoreCase))
                return "Cancelled";

            return "Booked";
        }

        private List<int> GetRelevantDoctorIds(Assistant assistant, Clinic clinic)
        {
            // Only show doctors who explicitly invited this assistant and she accepted.
            // Intersect with clinic doctors as a safety check (both sides must agree).
            var assistantDoctorIds = assistant.AssistantDoctors?
                .Select(ad => ad.DoctorID).ToHashSet() ?? new HashSet<int>();
            var clinicDoctorIds = clinic.ClinicDoctors?
                .Select(cd => cd.DoctorID).ToHashSet() ?? new HashSet<int>();
            return assistantDoctorIds.Intersect(clinicDoctorIds).ToList();
        }

        private List<AssistantDoctorSummary> BuildDoctorSummaries(
            Assistant assistant, Clinic clinic, List<int> relevantDoctorIds)
        {
            var summaries = new List<AssistantDoctorSummary>();
            foreach (var dId in relevantDoctorIds)
            {
                var clinicDoctor = clinic.ClinicDoctors?.FirstOrDefault(cd => cd.DoctorID == dId);
                var doctor = clinicDoctor?.Doctor;
                var fullName = doctor?.User != null
                    ? $"Dr. {doctor.User.FirstName} {doctor.User.LastName}".Trim()
                    : "Doctor";

                if (doctor == null)
                {
                    var ad = assistant.AssistantDoctors?.FirstOrDefault(a => a.DoctorID == dId);
                    if (ad?.Doctor?.User != null)
                    {
                        doctor = ad.Doctor;
                        fullName = $"Dr. {doctor.User.FirstName} {doctor.User.LastName}".Trim();
                    }
                }

                summaries.Add(new AssistantDoctorSummary
                {
                    DoctorID = dId,
                    FullName = fullName,
                    Specialization = doctor?.Specialization ?? "General Practitioner"
                });
            }
            return summaries;
        }

        public IActionResult Patients(int id, int? doctorId)
        {
            var accessResult = TryResolveAssistantClinic(id, out var assistant, out var clinic);
            if (accessResult != null) return accessResult;

            var relevantDoctorIds = GetRelevantDoctorIds(assistant, clinic);
            var doctorSummaries = BuildDoctorSummaries(assistant, clinic, relevantDoctorIds);

            bool isFiltered = doctorId.HasValue && relevantDoctorIds.Contains(doctorId.Value);
            var activeDoctorIds = isFiltered
                ? new List<int> { doctorId!.Value }
                : relevantDoctorIds;

            var approvedLinks = _patientDoctorRepository
                .GetApprovedByDoctors(activeDoctorIds)
                .Where(pd => pd.Patient != null)
                .GroupBy(pd => new { pd.DoctorID, pd.PatientID })
                .Select(g => g.First())
                .ToList();

            var approvedPatientIds = approvedLinks
                .Select(pd => pd.PatientID)
                .Distinct()
                .ToHashSet();

            var appointments = _appointmentRepository
                .GetBookedByClinicAndDoctors(clinic.ClinicID, activeDoctorIds)
                .Where(a => a.PatientID.HasValue && approvedPatientIds.Contains(a.PatientID.Value))
                .ToList();

            var patientRows = approvedLinks
                .Select(link =>
                {
                    var patient = link.Patient!;
                    var patientAppointments = appointments
                        .Where(a => a.DoctorID == link.DoctorID && a.PatientID == link.PatientID)
                        .OrderByDescending(a => a.Date)
                        .ThenByDescending(a => a.Time)
                        .ToList();

                    var firstName = patient.User?.FirstName?.Trim();
                    var lastName = patient.User?.LastName?.Trim();
                    var fullName = string.Join(" ", new[] { firstName, lastName }
                        .Where(x => !string.IsNullOrWhiteSpace(x))).Trim();

                    var doctor = link.Doctor;
                    var doctorFullName = doctor?.User != null
                        ? $"Dr. {doctor.User.FirstName} {doctor.User.LastName}".Trim()
                        : "Doctor";

                    return new AssistantPatientAppointmentsSummary
                    {
                        PatientID = patient.PatientID,
                        FullName = string.IsNullOrWhiteSpace(fullName) ? "Patient" : fullName,
                        PhoneNumber = patient.User?.PhoneNumber,
                        DoctorID = link.DoctorID,
                        DoctorName = doctorFullName,
                        DoctorSpecialization = doctor?.Specialization ?? string.Empty,
                        Appointments = patientAppointments
                    };
                })
                .OrderBy(row => row.DoctorName)
                .ThenBy(row => row.FullName)
                .ToList();

            var selectedDoctorName = isFiltered
                ? doctorSummaries.FirstOrDefault(d => d.DoctorID == doctorId!.Value)?.FullName ?? "Doctor"
                : "All Doctors";

            var viewModel = new AssistantPatientsViewModel
            {
                Assistant = assistant,
                AssistantName = BuildAssistantDisplayName(assistant.User),
                Clinic = clinic,
                ClinicName = clinic.Name ?? "Clinic",
                Doctors = doctorSummaries,
                SelectedDoctorID = isFiltered ? doctorId : null,
                SelectedDoctorName = selectedDoctorName,
                Patients = patientRows
            };

            return View(viewModel);
        }

        public IActionResult PatientDetails(int id, int patientId, int? doctorId)
        {
            var accessResult = TryResolveAssistantClinic(id, out var assistant, out var clinic);
            if (accessResult != null) return accessResult;

            var relevantDoctorIds = GetRelevantDoctorIds(assistant, clinic);
            var activeDoctorIds = doctorId.HasValue && relevantDoctorIds.Contains(doctorId.Value)
                ? new List<int> { doctorId.Value }
                : relevantDoctorIds;

            var approvedLinks = _patientDoctorRepository
                .GetApprovedByDoctors(activeDoctorIds)
                .Where(pd => pd.PatientID == patientId)
                .ToList();

            if (!approvedLinks.Any())
                return Forbid();

            var patient = approvedLinks
                .Select(pd => pd.Patient)
                .FirstOrDefault(p => p != null);

            if (patient == null)
                return NotFound();

            var assignedDoctorIds = approvedLinks
                .Select(pd => pd.DoctorID)
                .Distinct()
                .ToList();

            var appointments = _appointmentRepository
                .GetBookedByClinicAndDoctors(clinic.ClinicID, assignedDoctorIds)
                .Where(a => a.PatientID == patientId)
                .OrderByDescending(a => a.Date)
                .ThenByDescending(a => a.Time)
                .ToList();

            var medications = _context.PatientDrugs
                .AsNoTracking()
                .Where(d => d.PatientID == patientId)
                .OrderByDescending(d => d.DrugID)
                .ToList();

            var pregnancyRecords = _context.PregnancyRecords
                .AsNoTracking()
                .Where(r => r.PatientID == patientId)
                .OrderByDescending(r => r.StartDate)
                .ToList();

            var assignedDoctors = approvedLinks
                .Select(pd => pd.Doctor)
                .Where(d => d != null)
                .GroupBy(d => d!.DoctorID)
                .Select(g => g.First()!)
                .ToList();

            var patientName = patient.User != null
                ? string.Join(" ", new[] { patient.User.FirstName, patient.User.LastName }
                    .Where(s => !string.IsNullOrWhiteSpace(s))).Trim()
                : "Patient";

            var patientAlerts = _alertRepository.GetByPatientId(patientId).ToList();

            var vm = new AssistantPatientDetailsViewModel
            {
                Assistant = assistant,
                AssistantName = BuildAssistantDisplayName(assistant.User),
                Clinic = clinic,
                ClinicName = clinic.Name ?? "Clinic",
                Patient = patient,
                PatientName = string.IsNullOrWhiteSpace(patientName) ? "Patient" : patientName,
                AssignedDoctors = assignedDoctors,
                Appointments = appointments,
                Medications = medications,
                PregnancyRecords = pregnancyRecords,
                Alerts = patientAlerts
            };

            return View(vm);
        }

        public IActionResult Appointments(int id, int? doctorId, DateTime? date)
        {
            var accessResult = TryResolveAssistantClinic(id, out var assistant, out var clinic);
            if (accessResult != null) return accessResult;

            var relevantDoctorIds = GetRelevantDoctorIds(assistant, clinic);
            var doctorSummaries = BuildDoctorSummaries(assistant, clinic, relevantDoctorIds);
            bool isFiltered = doctorId.HasValue && relevantDoctorIds.Contains(doctorId.Value);
            var selectedDate = date?.Date ?? DateTime.Today;

            var viewModel = new AssistantAppointmentsViewModel
            {
                Assistant = assistant,
                AssistantName = BuildAssistantDisplayName(assistant.User),
                Clinic = clinic,
                ClinicName = clinic.Name ?? "Clinic",
                SelectedDate = selectedDate,
                Doctors = doctorSummaries,
                SelectedDoctorID = isFiltered ? doctorId : null
            };

            return View(viewModel);
        }

        [HttpGet]
        public IActionResult GetAppointments(int id, int? doctorId, string status = "Confirmed", string? date = null, int page = 1, int pageSize = 20, string? search = null)
        {
            var accessResult = TryResolveAssistantClinic(id, out _, out _, true);
            if (accessResult != null) return accessResult;

            var targetDate = ParseDashboardDate(date);
            var scope = _assistantScheduleService.BuildScope(id, doctorId);
            if (scope == null) return NotFound();

            var result = _assistantScheduleService.GetAppointmentsPage(scope, status, targetDate, page, pageSize, search);
            return Json(new
            {
                items = result.Items,
                total = result.Total,
                page = result.Page,
                pageSize = result.PageSize,
                totalPages = result.TotalPages
            });
        }

        /// <summary>
        /// Lightweight endpoint that returns only the counts per status,
        /// avoiding the cost of serializing full appointment objects.
        /// </summary>
        [HttpGet]
        public IActionResult GetAppointmentCounts(int id, int? doctorId, string? date = null)
        {
            var accessResult = TryResolveAssistantClinic(id, out _, out _, true);
            if (accessResult != null) return accessResult;

            var targetDate = ParseDashboardDate(date);
            var scope = _assistantScheduleService.BuildScope(id, doctorId);
            if (scope == null) return NotFound();

            var counts = _assistantScheduleService.GetCounts(scope, targetDate);

            return Json(new
            {
                confirmed = counts.Confirmed,
                modified = counts.Modified,
                cancelled = counts.Cancelled,
                missed = counts.Missed,
                total = counts.Total
            });
        }

        [HttpGet]
        public IActionResult GetAppointmentDetail(int id, int appointmentId)
        {
            var accessResult = TryResolveAssistantClinic(id, out var assistant, out var clinic, true);
            if (accessResult != null) return accessResult;

            var appointment = _appointmentRepository.GetByIdWithBooking(appointmentId);
            if (appointment == null || appointment.ClinicID != clinic.ClinicID)
                return NotFound();

            var relevantDoctorIds = GetRelevantDoctorIds(assistant, clinic);
            if (!relevantDoctorIds.Contains(appointment.DoctorID))
                return Forbid();

            return Json(new
            {
                appointmentId = appointment.AppointmentID,
                doctorId = appointment.DoctorID,
                patientName = appointment.Patient?.User != null
                    ? $"{appointment.Patient.User.FirstName} {appointment.Patient.User.LastName}" : "Unknown",
                doctorName = appointment.Doctor?.User != null
                    ? $"Dr. {appointment.Doctor.User.FirstName} {appointment.Doctor.User.LastName}" : "Unknown",
                clinicName = appointment.Clinic?.Name ?? string.Empty,
                clinicLocation = appointment.Clinic?.Location ?? string.Empty,
                date = appointment.Date.ToString("yyyy-MM-dd"),
                time = appointment.Time.ToString(@"hh\:mm"),
                status = appointment.Booking?.Status ?? "Confirmed",
                reason = appointment.Booking?.Reason ?? string.Empty,
                notes = appointment.Booking?.Notes ?? string.Empty
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ModifyAppointment(int id, int appointmentId, string newDate, string newTime, string reason)
        {
            var accessResult = TryResolveAssistantClinic(id, out var assistant, out var clinic, true);
            if (accessResult != null) return accessResult;

            var appointment = _appointmentRepository.GetByIdWithBooking(appointmentId);
            if (appointment == null || appointment.ClinicID != clinic.ClinicID)
                return Json(new { success = false, message = "Appointment not found." });

            var relevantDoctorIds = GetRelevantDoctorIds(assistant, clinic);
            if (!relevantDoctorIds.Contains(appointment.DoctorID))
                return Json(new { success = false, message = "Access denied." });

            if (!DateTime.TryParse(newDate, out var parsedDate))
                return Json(new { success = false, message = "Invalid date." });

            if (!TimeSpan.TryParse(newTime, out var parsedTime))
                return Json(new { success = false, message = "Invalid time." });

            if (parsedDate.Date < DateTime.Today)
                return Json(new { success = false, message = "Cannot schedule an appointment in the past." });

            if (parsedDate.Date == DateTime.Today && parsedTime <= DateTime.Now.TimeOfDay)
                return Json(new { success = false, message = "Cannot schedule an appointment in the past time today." });

            if (_appointmentRepository.HasDoctorConflict(appointment.DoctorID, parsedDate, parsedTime, appointmentId))
                return Json(new { success = false, message = "The doctor already has an appointment at this time in another clinic." });

            var modifyPatientId = appointment.Booking?.PatientID ?? appointment.Patient?.PatientID ?? 0;
            var modifyPatientName = appointment.Patient?.User != null
                ? $"{appointment.Patient.User.FirstName} {appointment.Patient.User.LastName}".Trim()
                : "Patient";
            var modifyDoctorName = appointment.Doctor?.User != null
                ? $"Dr. {appointment.Doctor.User.FirstName} {appointment.Doctor.User.LastName}".Trim()
                : "Doctor";

            appointment.Date = parsedDate;
            appointment.Time = parsedTime;
            _appointmentRepository.Update(appointment);

            if (appointment.Booking != null)
            {
                appointment.Booking.Status = "Modified";
                if (!string.IsNullOrWhiteSpace(reason))
                    appointment.Booking.Notes = reason;
                _bookingRepository.Update(appointment.Booking);
            }

            _appointmentRepository.Save();

            if (modifyPatientId > 0)
            {
                var reasonNote = string.IsNullOrWhiteSpace(reason) ? "" : $" Reason: {reason}";
                CreateOperationalAlert(modifyPatientId,
                    "Appointment Rescheduled",
                    $"Appointment for {modifyPatientName} with {modifyDoctorName} has been rescheduled to {parsedDate:MMM dd, yyyy} at {parsedTime:hh\\:mm}.{reasonNote}",
                    AlertTypes.Warning);
            }

            return Json(new { success = true, message = "Appointment modified successfully." });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CancelAppointment(int id, int appointmentId, string reason)
        {
            var accessResult = TryResolveAssistantClinic(id, out var assistant, out var clinic, true);
            if (accessResult != null) return accessResult;

            var appointment = _appointmentRepository.GetByIdWithBooking(appointmentId);
            if (appointment == null || appointment.ClinicID != clinic.ClinicID)
                return Json(new { success = false, message = "Appointment not found." });

            var relevantDoctorIds = GetRelevantDoctorIds(assistant, clinic);
            if (!relevantDoctorIds.Contains(appointment.DoctorID))
                return Json(new { success = false, message = "Access denied." });

            var cancelPatientId = appointment.Booking?.PatientID ?? appointment.Patient?.PatientID ?? 0;
            var cancelPatientName = appointment.Patient?.User != null
                ? $"{appointment.Patient.User.FirstName} {appointment.Patient.User.LastName}".Trim()
                : "Patient";
            var cancelDoctorName = appointment.Doctor?.User != null
                ? $"Dr. {appointment.Doctor.User.FirstName} {appointment.Doctor.User.LastName}".Trim()
                : "Doctor";
            var cancelDate = appointment.Date;

            appointment.isBooked = false;
            appointment.PatientID = null;
            _appointmentRepository.Update(appointment);

            if (appointment.Booking != null)
            {
                appointment.Booking.Status = "Cancelled";
                appointment.Booking.IsActive = false;
                if (!string.IsNullOrWhiteSpace(reason))
                    appointment.Booking.Notes = reason;
                _bookingRepository.Update(appointment.Booking);
            }

            _appointmentRepository.Save();

            if (cancelPatientId > 0)
            {
                var reasonNote = string.IsNullOrWhiteSpace(reason) ? "" : $" Reason: {reason}";
                CreateOperationalAlert(cancelPatientId,
                    "Appointment Cancelled",
                    $"Appointment for {cancelPatientName} with {cancelDoctorName} on {cancelDate:MMM dd, yyyy} has been cancelled.{reasonNote}",
                    AlertTypes.Warning);
            }

            return Json(new { success = true, message = "Appointment cancelled successfully." });
        }

        [HttpGet]
        public IActionResult ExportAppointmentsCsv(int id, int? doctorId, string status = "Confirmed", string? date = null, string? search = null)
        {
            var accessResult = TryResolveAssistantClinic(id, out _, out _, true);
            if (accessResult != null) return accessResult;

            var scope = _assistantScheduleService.BuildScope(id, doctorId);
            if (scope == null) return NotFound();

            var targetDate = ParseDashboardDate(date);

            var appointments = _appointmentRepository
                .GetPagedByClinicDoctorsStatusAndDate(scope.ClinicId, scope.ActiveDoctorIds, status, targetDate, search, 1, 1000)
                .ToList();

            var lines = new System.Text.StringBuilder();
            lines.AppendLine("Appointment ID,Patient Name,Patient Phone,Doctor,Specialization,Date,Time,Status,Reason,Notes");

            foreach (var a in appointments)
            {
                var patientName = a.Patient?.User != null
                    ? $"{a.Patient.User.FirstName} {a.Patient.User.LastName}".Trim() : "Unknown";
                var phone = a.Patient?.User?.PhoneNumber ?? string.Empty;
                var doctorName = a.Doctor?.User != null
                    ? $"Dr. {a.Doctor.User.FirstName} {a.Doctor.User.LastName}".Trim() : "Unknown";
                var spec = a.Doctor?.Specialization ?? string.Empty;
                var aStatus = a.Booking?.Status ?? "Confirmed";
                var reason = a.Booking?.Reason ?? string.Empty;
                var notes = a.Booking?.Notes ?? string.Empty;

                string Esc(string s) => $"\"{s.Replace("\"", "\"\"")}\"";
                lines.AppendLine($"{a.AppointmentID},{Esc(patientName)},{Esc(phone)},{Esc(doctorName)},{Esc(spec)},{a.Date:yyyy-MM-dd},{a.Time:hh\\:mm},{Esc(aStatus)},{Esc(reason)},{Esc(notes)}");
            }

            var bytes = System.Text.Encoding.UTF8.GetPreamble().Concat(System.Text.Encoding.UTF8.GetBytes(lines.ToString())).ToArray();
            var fileName = $"appointments_{targetDate:yyyy-MM-dd}_{status}.csv";
            return File(bytes, "text/csv", fileName);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ReinstateAppointment(int id, int appointmentId)
        {
            var accessResult = TryResolveAssistantClinic(id, out var assistant, out var clinic, true);
            if (accessResult != null) return accessResult;

            var appointment = _appointmentRepository.GetByIdWithBooking(appointmentId);
            if (appointment == null || appointment.ClinicID != clinic.ClinicID)
                return Json(new { success = false, message = "Appointment not found." });

            var relevantDoctorIds = GetRelevantDoctorIds(assistant, clinic);
            if (!relevantDoctorIds.Contains(appointment.DoctorID))
                return Json(new { success = false, message = "Access denied." });

            if (appointment.Booking == null)
                return Json(new { success = false, message = "No booking found for this appointment." });

            appointment.isBooked = true;
            if (appointment.Booking.PatientID > 0)
                appointment.PatientID = appointment.Booking.PatientID;

            appointment.Booking.Status = "Confirmed";
            appointment.Booking.IsActive = true;

            _appointmentRepository.Update(appointment);
            _bookingRepository.Update(appointment.Booking);
            _appointmentRepository.Save();

            var reinstatePatientId = appointment.Booking.PatientID > 0 ? appointment.Booking.PatientID : (appointment.Patient?.PatientID ?? 0);
            if (reinstatePatientId > 0)
            {
                var reinstatePatientName = appointment.Patient?.User != null
                    ? $"{appointment.Patient.User.FirstName} {appointment.Patient.User.LastName}".Trim()
                    : "Patient";
                var reinstateDoctorName = appointment.Doctor?.User != null
                    ? $"Dr. {appointment.Doctor.User.FirstName} {appointment.Doctor.User.LastName}".Trim()
                    : "Doctor";
                CreateOperationalAlert(reinstatePatientId,
                    "Appointment Reinstated",
                    $"Appointment for {reinstatePatientName} with {reinstateDoctorName} on {appointment.Date:MMM dd, yyyy} has been reinstated.",
                    AlertTypes.Info);
            }

            return Json(new { success = true, message = "Appointment reinstated successfully." });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CheckInPatient(int id, int appointmentId)
        {
            var accessResult = TryResolveAssistantClinic(id, out var assistant, out var clinic, true);
            if (accessResult != null) return accessResult;

            var appointment = _appointmentRepository.GetByIdWithBooking(appointmentId);
            if (appointment == null || appointment.ClinicID != clinic.ClinicID)
                return Json(new { success = false, message = "Appointment not found." });

            var relevantDoctorIds = GetRelevantDoctorIds(assistant, clinic);
            if (!relevantDoctorIds.Contains(appointment.DoctorID))
                return Json(new { success = false, message = "Access denied." });

            if (appointment.Booking == null)
                return Json(new { success = false, message = "No booking found for this appointment." });

            appointment.Booking.IsCheckedIn = true;
            appointment.Booking.CheckedInAt = DateTime.Now;
            _bookingRepository.Update(appointment.Booking);
            _appointmentRepository.Save();

            var checkInPatientId = appointment.Booking.PatientID > 0 ? appointment.Booking.PatientID : (appointment.Patient?.PatientID ?? 0);
            if (checkInPatientId > 0)
            {
                var checkInPatientName = appointment.Patient?.User != null
                    ? $"{appointment.Patient.User.FirstName} {appointment.Patient.User.LastName}".Trim()
                    : "Patient";
                var checkInDoctorName = appointment.Doctor?.User != null
                    ? $"Dr. {appointment.Doctor.User.FirstName} {appointment.Doctor.User.LastName}".Trim()
                    : "Doctor";
                CreateOperationalAlert(checkInPatientId,
                    "Patient Checked In",
                    $"{checkInPatientName} has checked in for their appointment with {checkInDoctorName} on {appointment.Date:MMM dd, yyyy}.",
                    AlertTypes.Info);
            }

            return Json(new
            {
                success = true,
                message = "Patient checked in.",
                checkedInAt = appointment.Booking.CheckedInAt?.ToString("hh:mm tt")
            });
        }

        public IActionResult Alerts(int id)
        {
            var accessResult = TryResolveAssistantClinic(id, out var assistant, out var clinic);
            if (accessResult != null) return accessResult;

            var relevantDoctorIds = GetRelevantDoctorIds(assistant, clinic);
            var patientIds = _patientDoctorRepository
                .GetApprovedByDoctors(relevantDoctorIds)
                .Select(pd => pd.PatientID)
                .Distinct()
                .ToList();

            var alerts = _patientNotificationService
                .GetForPatients(patientIds, PatientNotificationTypes.Operational);

            var vm = new AssistantAlertsViewModel
            {
                Assistant = assistant,
                AssistantName = BuildAssistantDisplayName(assistant.User),
                ClinicName = clinic.Name ?? "Clinic",
                Alerts = alerts,
                UnreadCount = alerts.Count(a => !a.IsRead)
            };

            ViewData["Title"] = "Alerts";
            ViewData["AssistantId"] = assistant.AssistantID;
            ViewData["AssistantName"] = vm.AssistantName;
            ViewData["ActivePage"] = "Alerts";
            ViewData["PageTitle"] = "Clinic Notifications";
            ViewData["PageSubtitle"] = "Appointment and patient activity for " + clinic.Name;

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult MarkAlertRead(int id, int alertId)
        {
            var accessResult = TryResolveAssistantClinic(id, out var assistant, out var clinic, true);
            if (accessResult != null) return accessResult;

            var notification = _context.PatientNotifications.Find(alertId);
            if (notification == null)
                return Json(new { success = false, message = "Notification not found." });

            var relevantDoctorIds = GetRelevantDoctorIds(assistant, clinic);
            var patientIds = _patientDoctorRepository
                .GetApprovedByDoctors(relevantDoctorIds)
                .Select(pd => pd.PatientID)
                .ToHashSet();

            if (!patientIds.Contains(notification.PatientID))
                return Json(new { success = false, message = "Access denied." });

            _patientNotificationService.MarkRead(alertId);

            return Json(new { success = true });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult MarkAllAlertsRead(int id)
        {
            var accessResult = TryResolveAssistantClinic(id, out var assistant, out var clinic, true);
            if (accessResult != null) return accessResult;

            var relevantDoctorIds = GetRelevantDoctorIds(assistant, clinic);
            var patientIds = _patientDoctorRepository
                .GetApprovedByDoctors(relevantDoctorIds)
                .Select(pd => pd.PatientID)
                .ToList();

            var count = _patientNotificationService.MarkAllRead(patientIds, PatientNotificationTypes.Operational);

            return Json(new { success = true, count });
        }

        public IActionResult Availability(int id, int? doctorId)
        {
            var accessResult = TryResolveAssistantClinic(id, out var assistant, out var clinic);
            if (accessResult != null) return accessResult;

            var relevantDoctorIds = GetRelevantDoctorIds(assistant, clinic);
            var doctorSummaries = BuildDoctorSummaries(assistant, clinic, relevantDoctorIds);
            bool isFiltered = doctorId.HasValue && relevantDoctorIds.Contains(doctorId.Value);

            var viewModel = new AssistantAvailabilityViewModel
            {
                Assistant = assistant,
                AssistantName = BuildAssistantDisplayName(assistant.User),
                Clinic = clinic,
                ClinicName = clinic.Name ?? "Clinic",
                Doctors = doctorSummaries,
                SelectedDoctorID = isFiltered ? doctorId : (relevantDoctorIds.Count == 1 ? relevantDoctorIds[0] : null)
            };

            return View(viewModel);
        }

        [HttpGet]
        public IActionResult GetAvailabilitySlots(int id, int doctorId, string date)
        {
            var accessResult = TryResolveAssistantClinic(id, out var assistant, out var clinic, true);
            if (accessResult != null) return accessResult;

            var relevantDoctorIds = GetRelevantDoctorIds(assistant, clinic);
            if (!relevantDoctorIds.Contains(doctorId))
                return Forbid();

            if (!DateTime.TryParse(date, out var parsedDate))
                return BadRequest("Invalid date.");

            var appointments = _appointmentRepository.GetByClinicDoctorAndDate(clinic.ClinicID, doctorId, parsedDate);

            var otherClinicSlots = _appointmentRepository
                .GetByDoctorAndDate(doctorId, parsedDate)
                .Where(a => a.ClinicID != clinic.ClinicID)
                .Select(a => new
                {
                    time = a.Time.ToString(@"hh\:mm"),
                    isBooked = a.isBooked,
                    clinicName = a.Clinic?.Name ?? "Other Clinic"
                }).ToList();

            var result = appointments.Select(a => new
            {
                appointmentId = a.AppointmentID,
                time = a.Time.ToString(@"hh\:mm"),
                isBooked = a.isBooked,
                patientName = a.Patient?.User != null
                    ? $"{a.Patient.User.FirstName} {a.Patient.User.LastName}" : null,
                clinicName = clinic.Name ?? "Clinic",
                clinicLocation = clinic.Location ?? string.Empty
            }).ToList();

            return Json(new { slots = result, otherClinicSlots });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CreateAvailabilitySlot(int id, int doctorId, string date, string time)
        {
            var accessResult = TryResolveAssistantClinic(id, out var assistant, out var clinic, true);
            if (accessResult != null) return accessResult;

            var relevantDoctorIds = GetRelevantDoctorIds(assistant, clinic);
            if (!relevantDoctorIds.Contains(doctorId))
                return Json(new { success = false, message = "Access denied." });

            if (!DateTime.TryParse(date, out var parsedDate))
                return Json(new { success = false, message = "Invalid date." });

            if (!TimeSpan.TryParse(time, out var parsedTime))
                return Json(new { success = false, message = "Invalid time." });

            if (parsedDate.Date < DateTime.Today)
                return Json(new { success = false, message = "Cannot create slots in the past." });

            if (parsedDate.Date == DateTime.Today && parsedTime <= DateTime.Now.TimeOfDay)
                return Json(new { success = false, message = "Cannot create slots for past times today." });

            var existingAcrossAllClinics = _appointmentRepository
                .GetByDoctorAndDate(doctorId, parsedDate)
                .Any(a => a.Time == parsedTime);
            if (existingAcrossAllClinics)
                return Json(new { success = false, message = "The doctor already has a slot at this time in another clinic." });

            var slot = new Appointment
            {
                DoctorID = doctorId,
                ClinicID = clinic.ClinicID,
                PatientID = null,
                Date = parsedDate,
                Time = parsedTime,
                isBooked = false,
                CreatedByAssistantID = id
            };
            _appointmentRepository.Add(slot);
            _appointmentRepository.Save();

            return Json(new { success = true, message = "Slot created.", appointmentId = slot.AppointmentID });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteAvailabilitySlot(int id, int appointmentId)
        {
            var accessResult = TryResolveAssistantClinic(id, out var assistant, out var clinic, true);
            if (accessResult != null) return accessResult;

            var appointment = _appointmentRepository.GetById(appointmentId);
            if (appointment == null || appointment.ClinicID != clinic.ClinicID)
                return Json(new { success = false, message = "Slot not found." });

            if (appointment.isBooked)
                return Json(new { success = false, message = "Cannot remove a booked slot." });

            var relevantDoctorIds = GetRelevantDoctorIds(assistant, clinic);
            if (!relevantDoctorIds.Contains(appointment.DoctorID))
                return Json(new { success = false, message = "Access denied." });

            _appointmentRepository.Delete(appointmentId);
            _appointmentRepository.Save();

            return Json(new { success = true, message = "Slot removed." });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SetAllSlotsAvailable(int id, int doctorId, string date,
            string startTime, string endTime, int slotDuration)
        {
            var accessResult = TryResolveAssistantClinic(id, out var assistant, out var clinic, true);
            if (accessResult != null) return accessResult;

            var relevantDoctorIds = GetRelevantDoctorIds(assistant, clinic);
            if (!relevantDoctorIds.Contains(doctorId))
                return Json(new { success = false, message = "Access denied." });

            if (!DateTime.TryParse(date, out var parsedDate))
                return Json(new { success = false, message = "Invalid date." });

            if (parsedDate.Date < DateTime.Today)
                return Json(new { success = false, message = "Cannot create slots in the past." });

            if (!TimeSpan.TryParse(startTime, out var start) || !TimeSpan.TryParse(endTime, out var end))
                return Json(new { success = false, message = "Invalid time range." });

            if (slotDuration <= 0)
                return Json(new { success = false, message = "Slot duration must be greater than 0." });

            if (start >= end)
                return Json(new { success = false, message = "Start time must be earlier than end time." });

            var nowTime = DateTime.Now.TimeOfDay;

            var existingTimes = _appointmentRepository
                .GetByDoctorAndDate(doctorId, parsedDate)
                .Select(a => a.Time).ToHashSet();

            var newSlots = new List<Appointment>();
            var current = start;
            while (current < end)
            {
                if (parsedDate.Date == DateTime.Today && current <= nowTime)
                {
                    current = current.Add(TimeSpan.FromMinutes(slotDuration));
                    continue;
                }

                if (!existingTimes.Contains(current))
                {
                    newSlots.Add(new Appointment
                    {
                        DoctorID = doctorId,
                        ClinicID = clinic.ClinicID,
                        PatientID = null,
                        Date = parsedDate,
                        Time = current,
                        isBooked = false,
                        CreatedByAssistantID = id
                    });
                }
                current = current.Add(TimeSpan.FromMinutes(slotDuration));
            }

            if (newSlots.Any())
            {
                _appointmentRepository.AddRange(newSlots);
                _appointmentRepository.Save();
            }

            return Json(new { success = true, message = $"{newSlots.Count} slot(s) created." });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult BlockAllAvailabilitySlots(int id, int doctorId, string date)
        {
            var accessResult = TryResolveAssistantClinic(id, out var assistant, out var clinic, true);
            if (accessResult != null) return accessResult;

            var relevantDoctorIds = GetRelevantDoctorIds(assistant, clinic);
            if (!relevantDoctorIds.Contains(doctorId))
                return Json(new { success = false, message = "Access denied." });

            if (!DateTime.TryParse(date, out var parsedDate))
                return Json(new { success = false, message = "Invalid date." });

            var slots = _appointmentRepository
                .GetByClinicDoctorAndDate(clinic.ClinicID, doctorId, parsedDate)
                .Where(a => !a.isBooked).ToList();

            foreach (var slot in slots)
                _appointmentRepository.Delete(slot.AppointmentID);

            _appointmentRepository.Save();

            return Json(new { success = true, message = $"{slots.Count} slot(s) blocked." });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ApplyQuickSetupSchedule(int id, [FromBody] QuickSetupRequest request)
        {
            var accessResult = TryResolveAssistantClinic(id, out var assistant, out var clinic, true);
            if (accessResult != null) return accessResult;

            var relevantDoctorIds = GetRelevantDoctorIds(assistant, clinic);
            if (!relevantDoctorIds.Contains(request.DoctorId))
                return Json(new { success = false, message = "Access denied." });

            if (request.WorkingDays == null || !request.WorkingDays.Any())
                return Json(new { success = false, message = "Please select at least one working day." });

            if (!TimeSpan.TryParse(request.StartTime, out var start) || !TimeSpan.TryParse(request.EndTime, out var end))
                return Json(new { success = false, message = "Invalid time range." });

            if (request.SlotDuration <= 0)
                return Json(new { success = false, message = "Slot duration must be greater than 0." });

            if (request.WeeksAhead <= 0)
                return Json(new { success = false, message = "Weeks ahead must be greater than 0." });

            if (start >= end)
                return Json(new { success = false, message = "Start time must be earlier than end time." });

            var today = DateTime.Today;
            var endDate = today.AddDays(request.WeeksAhead * 7);
            var nowTime = DateTime.Now.TimeOfDay;

            var existingSet = _appointmentRepository
                .GetByDoctorAndDateRange(request.DoctorId, today, endDate)
                .Select(a => (a.Date.Date, a.Time))
                .ToHashSet();

            var newSlots = new List<Appointment>();
            for (var d = today; d <= endDate; d = d.AddDays(1))
            {
                var isTodayDate = d.Date == today;
                if (!isTodayDate && !request.WorkingDays.Contains((int)d.DayOfWeek)) continue;

                var current = start;
                while (current < end)
                {
                    if (d.Date == today && current <= nowTime)
                    {
                        current = current.Add(TimeSpan.FromMinutes(request.SlotDuration));
                        continue;
                    }

                    if (!existingSet.Contains((d, current)))
                    {
                        newSlots.Add(new Appointment
                        {
                            DoctorID = request.DoctorId,
                            ClinicID = clinic.ClinicID,
                            PatientID = null,
                            Date = d,
                            Time = current,
                            isBooked = false,
                            CreatedByAssistantID = id
                        });
                    }
                    current = current.Add(TimeSpan.FromMinutes(request.SlotDuration));
                }
            }

            if (newSlots.Any())
            {
                _appointmentRepository.AddRange(newSlots);
                _appointmentRepository.Save();
            }

            return Json(new { success = true, message = $"Schedule applied. {newSlots.Count} slot(s) created." });
        }


        [HttpGet]
        public IActionResult CreatePatient(int id)
        {
            var accessResult = TryResolveAssistantClinic(id, out var assistant, out var clinic);
            if (accessResult != null) return accessResult;

            var relevantDoctorIds = GetRelevantDoctorIds(assistant, clinic);
            var doctorSummaries = BuildDoctorSummaries(assistant, clinic, relevantDoctorIds);

            var viewModel = new AssistantCreatePatientViewModel
            {
                Assistant = assistant,
                AssistantName = BuildAssistantDisplayName(assistant.User),
                Clinic = clinic,
                ClinicName = clinic.Name ?? "Clinic",
                Doctors = doctorSummaries
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreatePatient(int id, AssistantCreatePatientViewModel model)
        {
            var accessResult = TryResolveAssistantClinic(id, out var assistant, out var clinic);
            if (accessResult != null) return accessResult;

            var relevantDoctorIds = GetRelevantDoctorIds(assistant, clinic);
            var doctorSummaries = BuildDoctorSummaries(assistant, clinic, relevantDoctorIds);

            model.Assistant = assistant;
            model.AssistantName = BuildAssistantDisplayName(assistant.User);
            model.Clinic = clinic;
            model.ClinicName = clinic.Name ?? "Clinic";
            model.Doctors = doctorSummaries;

            if (string.IsNullOrWhiteSpace(model.FirstName) || string.IsNullOrWhiteSpace(model.LastName) ||
                string.IsNullOrWhiteSpace(model.Email) || string.IsNullOrWhiteSpace(model.PhoneNumber))
            {
                model.ErrorMessage = "Please fill in all required fields.";
                return View(model);
            }

            if (!model.SelectedDoctorID.HasValue || !relevantDoctorIds.Contains(model.SelectedDoctorID.Value))
            {
                model.ErrorMessage = "Invalid doctor selection.";
                return View(model);
            }

            var existingUser = await _userManager.FindByEmailAsync(model.Email);
            if (existingUser != null)
            {
                model.ErrorMessage = $"Email {model.Email} is already registered. Please use a different email or link the existing patient.";
                return View(model);
            }

            try
            {
                var tempPassword = GenerateTemporaryPassword();
                var user = new ApplicationUser
                {
                    UserName = model.Email,
                    Email = model.Email,
                    FirstName = model.FirstName.Trim(),
                    LastName = model.LastName.Trim(),
                    PhoneNumber = model.PhoneNumber.Trim(),
                    DateOfBirth = model.DateOfBirth ?? DateTime.UtcNow.Date,
                    IsActive = true,
                    CreatedDate = DateTime.UtcNow
                };

                var createResult = await _userManager.CreateAsync(user, tempPassword);
                if (!createResult.Succeeded)
                {
                    model.ErrorMessage = $"Failed to create user: {string.Join(", ", createResult.Errors.Select(e => e.Description))}";
                    return View(model);
                }

                await EnsureRoleAsync("Patient");
                await _userManager.AddToRoleAsync(user, "Patient");

                var patient = new Patient
                {
                    UserID = user.Id,
                    Address = model.Address ?? string.Empty,
                    WeightKg = model.WeightKg ?? 0,
                    HeightCm = model.HeightCm ?? 0,
                    BloodPressureIssue = model.BloodPressureIssue,
                    Smoking = model.Smoking,
                    AlcoholUse = model.AlcoholUse
                };

                if (model.IsPregnant && model.PregnancyDate.HasValue)
                {
                    patient.DateOfPregnancy = model.PregnancyDate;
                    patient.LastPregnancyStartedAt = model.PregnancyDate;
                    patient.IsFirstPregnancy = true;
                    patient.PregnancyCount = 1;
                    patient.GestationalWeeks = model.GestationalWeeks ?? 0;
                }

                _context.Patients.Add(patient);
                await _context.SaveChangesAsync();

                var patientDoctor = new PatientDoctor
                {
                    PatientID = patient.PatientID,
                    DoctorID = model.SelectedDoctorID.Value,
                    Status = "Approved",
                    RequestDate = DateTime.Now
                };

                _context.PatientDoctors.Add(patientDoctor);
                await _context.SaveChangesAsync();

                var newPatientDoctorName = doctorSummaries.FirstOrDefault(d => d.DoctorID == model.SelectedDoctorID.Value)?.FullName ?? "Doctor";
                var newPatientFullName = $"{model.FirstName} {model.LastName}".Trim();
                CreateOperationalAlert(patient.PatientID,
                    "New Patient Registered",
                    $"{newPatientFullName} has been registered and assigned to {newPatientDoctorName}.",
                    AlertTypes.Info,
                    alsoNotifyPatient: false);

                // Welcome notification for the new patient.
                _patientNotificationService.Notify(patient.PatientID,
                    "Welcome to NABD",
                    $"Welcome! Your account is ready and you've been assigned to {newPatientDoctorName}. Explore your dashboard to get started.",
                    PatientNotificationTypes.Account,
                    "/Patient/Index");

                if (model.IsPregnant && model.PregnancyDate.HasValue)
                {
                    _context.PregnancyRecords.Add(new PregnancyRecord
                    {
                        PatientID = patient.PatientID,
                        StartDate = model.PregnancyDate.Value,
                        CreatedAt = DateTime.Now
                    });
                    await _context.SaveChangesAsync();
                }

                var patientName = $"{model.FirstName} {model.LastName}".Trim();
                model.SuccessMessage = $"Patient {patientName} created. Login: {model.Email} | Temporary password: {tempPassword}";

                // Send temporary password to patient's email
                try
                {
                    var loginUrl = Url.Action("Login", "Account", null, Request.Scheme) ?? string.Empty;
                    var htmlBody = DoctorEmailTemplates.NewPatientWelcome(patientName, model.Email, tempPassword, loginUrl);
                    await _emailService.SendAsync(model.Email, patientName, "Welcome to NABD نبض — your account credentials", htmlBody);
                }
                catch
                {
                    // Email failures are logged by EmailService; do not block user creation
                }

                TempData["SuccessMessage"] = model.SuccessMessage;
                return RedirectToAction(nameof(Patients), new { id = id });
            }
            catch (Exception ex)
            {
                model.ErrorMessage = $"An error occurred: {ex.Message}";
                return View(model);
            }
        }

        private void CreateOperationalAlert(int patientId, string title, string message,
            string alertType = AlertTypes.Info, bool alsoNotifyPatient = true)
        {
            // Clinic-facing operational notification (shown to assistants).
            _patientNotificationService.Notify(patientId, title, message,
                PatientNotificationTypes.Operational, "/Assistant/Alerts", severity: alertType);

            // Patient-facing copy so the patient is informed of the appointment change too.
            if (alsoNotifyPatient)
            {
                _patientNotificationService.Notify(patientId, title, message,
                    PatientNotificationTypes.Appointment, "/Patient/Appointments", severity: alertType);
            }
        }

        private static string GenerateTemporaryPassword()
        {
            const string upper = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            const string lower = "abcdefghijklmnopqrstuvwxyz";
            const string digits = "0123456789";
            const string all = upper + lower + digits;

            var bytes = new byte[12];
            System.Security.Cryptography.RandomNumberGenerator.Fill(bytes);

            var password = new System.Text.StringBuilder();
            password.Append(upper[bytes[0] % upper.Length]);
            password.Append(lower[bytes[1] % lower.Length]);
            password.Append(digits[bytes[2] % digits.Length]);
            for (int i = 3; i < 12; i++)
                password.Append(all[bytes[i] % all.Length]);

            return password.ToString();
        }

        private async Task EnsureRoleAsync(string roleName)
        {
            if (!await _roleManager.RoleExistsAsync(roleName))
                await _roleManager.CreateAsync(new IdentityRole(roleName));
        }

        [HttpGet]
        public IActionResult CreateAppointment(int id)
        {
            var accessResult = TryResolveAssistantClinic(id, out var assistant, out var clinic);
            if (accessResult != null) return accessResult;

            var relevantDoctorIds = GetRelevantDoctorIds(assistant, clinic);
            var doctorSummaries = BuildDoctorSummaries(assistant, clinic, relevantDoctorIds);

            var approvedLinks = _patientDoctorRepository
                .GetApprovedByDoctors(relevantDoctorIds)
                .Where(pd => pd.Patient != null)
                .GroupBy(pd => pd.PatientID)
                .Select(g => g.First())
                .ToList();

            var existingPatients = approvedLinks
                .Select(link =>
                {
                    var patient = link.Patient!;
                    var firstName = patient.User?.FirstName?.Trim();
                    var lastName = patient.User?.LastName?.Trim();
                    var fullName = string.Join(" ", new[] { firstName, lastName }
                        .Where(x => !string.IsNullOrWhiteSpace(x))).Trim();

                    var doctor = link.Doctor;
                    var doctorFullName = doctor?.User != null
                        ? $"Dr. {doctor.User.FirstName} {doctor.User.LastName}".Trim()
                        : "Doctor";

                    return new AssistantPatientAppointmentsSummary
                    {
                        PatientID = patient.PatientID,
                        FullName = string.IsNullOrWhiteSpace(fullName) ? "Patient" : fullName,
                        PhoneNumber = patient.User?.PhoneNumber,
                        DoctorID = link.DoctorID,
                        DoctorName = doctorFullName,
                        Appointments = new List<Appointment>()
                    };
                })
                .OrderBy(p => p.FullName)
                .ToList();

            var viewModel = new AssistantCreateAppointmentViewModel
            {
                Assistant = assistant,
                AssistantName = BuildAssistantDisplayName(assistant.User),
                Clinic = clinic,
                ClinicName = clinic.Name ?? "Clinic",
                Doctors = doctorSummaries,
                ExistingPatients = existingPatients,
                PatientOption = "existing"
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateAppointment(int id, AssistantCreateAppointmentViewModel model)
        {
            var accessResult = TryResolveAssistantClinic(id, out var assistant, out var clinic);
            if (accessResult != null) return accessResult;

            var relevantDoctorIds = GetRelevantDoctorIds(assistant, clinic);
            var doctorSummaries = BuildDoctorSummaries(assistant, clinic, relevantDoctorIds);

            var approvedLinks = _patientDoctorRepository
                .GetApprovedByDoctors(relevantDoctorIds)
                .Where(pd => pd.Patient != null)
                .GroupBy(pd => pd.PatientID)
                .Select(g => g.First())
                .ToList();

            var existingPatients = approvedLinks
                .Select(link =>
                {
                    var patient = link.Patient!;
                    var firstName = patient.User?.FirstName?.Trim();
                    var lastName = patient.User?.LastName?.Trim();
                    var fullName = string.Join(" ", new[] { firstName, lastName }
                        .Where(x => !string.IsNullOrWhiteSpace(x))).Trim();

                    var doctor = link.Doctor;
                    var doctorFullName = doctor?.User != null
                        ? $"Dr. {doctor.User.FirstName} {doctor.User.LastName}".Trim()
                        : "Doctor";

                    return new AssistantPatientAppointmentsSummary
                    {
                        PatientID = patient.PatientID,
                        FullName = string.IsNullOrWhiteSpace(fullName) ? "Patient" : fullName,
                        PhoneNumber = patient.User?.PhoneNumber,
                        DoctorID = link.DoctorID,
                        DoctorName = doctorFullName,
                        Appointments = new List<Appointment>()
                    };
                })
                .OrderBy(p => p.FullName)
                .ToList();

            model.Assistant = assistant;
            model.AssistantName = BuildAssistantDisplayName(assistant.User);
            model.Clinic = clinic;
            model.ClinicName = clinic.Name ?? "Clinic";
            model.Doctors = doctorSummaries;
            model.ExistingPatients = existingPatients;

            if (!model.DoctorID.HasValue || !relevantDoctorIds.Contains(model.DoctorID.Value))
            {
                model.ErrorMessage = "Invalid doctor selection.";
                return View(model);
            }

            if (!model.AppointmentDate.HasValue || !model.AppointmentTime.HasValue)
            {
                model.ErrorMessage = "Please select appointment date and time.";
                return View(model);
            }

            var appointmentDateTime = model.AppointmentDate.Value.Add(model.AppointmentTime.Value);
            if (appointmentDateTime < DateTime.Now)
            {
                model.ErrorMessage = "Appointment cannot be in the past.";
                return View(model);
            }

            int patientId;
            string? newPatientTempPassword = null;

            if (model.PatientOption == "new")
            {
                if (string.IsNullOrWhiteSpace(model.NewPatientFirstName) ||
                    string.IsNullOrWhiteSpace(model.NewPatientLastName) ||
                    string.IsNullOrWhiteSpace(model.NewPatientEmail) ||
                    string.IsNullOrWhiteSpace(model.NewPatientPhoneNumber))
                {
                    model.ErrorMessage = "Please fill all required fields for new patient.";
                    return View(model);
                }

                var existingUser = await _userManager.FindByEmailAsync(model.NewPatientEmail);
                if (existingUser != null)
                {
                    model.ErrorMessage = $"Email {model.NewPatientEmail} is already registered.";
                    return View(model);
                }

                try
                {
                    var tempPassword = GenerateTemporaryPassword();
                    newPatientTempPassword = tempPassword;
                    var user = new ApplicationUser
                    {
                        UserName = model.NewPatientEmail,
                        Email = model.NewPatientEmail,
                        FirstName = model.NewPatientFirstName.Trim(),
                        LastName = model.NewPatientLastName.Trim(),
                        PhoneNumber = model.NewPatientPhoneNumber.Trim(),
                        DateOfBirth = model.NewPatientDateOfBirth ?? DateTime.UtcNow.Date,
                        IsActive = true,
                        CreatedDate = DateTime.UtcNow
                    };

                    var createResult = await _userManager.CreateAsync(user, tempPassword);
                    if (!createResult.Succeeded)
                    {
                        model.ErrorMessage = $"Failed to create user: {string.Join(", ", createResult.Errors.Select(e => e.Description))}";
                        return View(model);
                    }

                    await EnsureRoleAsync("Patient");
                    await _userManager.AddToRoleAsync(user, "Patient");

                    var patient = new Patient
                    {
                        UserID = user.Id,
                        Address = model.NewPatientAddress ?? string.Empty,
                        WeightKg = 0,
                        HeightCm = 0
                    };

                    _context.Patients.Add(patient);
                    await _context.SaveChangesAsync();

                    var patientDoctor = new PatientDoctor
                    {
                        PatientID = patient.PatientID,
                        DoctorID = model.DoctorID.Value,
                        Status = "Approved",
                        RequestDate = DateTime.Now
                    };

                    _context.PatientDoctors.Add(patientDoctor);
                    await _context.SaveChangesAsync();

                    patientId = patient.PatientID;
                    model.PatientName = $"{model.NewPatientFirstName} {model.NewPatientLastName}".Trim();

                    // Send welcome email with credentials
                    try
                    {
                        var loginUrl = Url.Action("Login", "Account", null, Request.Scheme) ?? string.Empty;
                        var htmlBody = DoctorEmailTemplates.NewPatientWelcome(model.PatientName, model.NewPatientEmail, tempPassword, loginUrl);
                        await _emailService.SendAsync(model.NewPatientEmail, model.PatientName, "Welcome to NABD نبض — your account credentials", htmlBody);
                    }
                    catch
                    {
                        // Email failures are logged by EmailService; do not block patient/appointment creation
                    }
                }
                catch (Exception ex)
                {
                    model.ErrorMessage = $"Failed to create patient: {ex.Message}";
                    return View(model);
                }
            }
            else
            {
                if (!model.ExistingPatientID.HasValue)
                {
                    model.ErrorMessage = "Please select a patient.";
                    return View(model);
                }

                var patientDoctorLink = approvedLinks.FirstOrDefault(pd => pd.PatientID == model.ExistingPatientID.Value);
                if (patientDoctorLink == null || patientDoctorLink.Patient == null)
                {
                    model.ErrorMessage = "Patient not found or not accessible.";
                    return View(model);
                }

                patientId = model.ExistingPatientID.Value;
                var patient = patientDoctorLink.Patient;
                model.PatientName = string.IsNullOrWhiteSpace(patient.User?.FirstName)
                    ? "Patient"
                    : $"{patient.User?.FirstName} {patient.User?.LastName}".Trim();
            }

            var conflictExists = _appointmentRepository
                .GetByDoctorAndDate(model.DoctorID.Value, model.AppointmentDate.Value)
                .Any(a => a.Time == model.AppointmentTime);

            if (conflictExists)
            {
                model.ErrorMessage = "Doctor already has an appointment at this time.";
                return View(model);
            }

            try
            {
                var appointment = new Appointment
                {
                    DoctorID = model.DoctorID.Value,
                    ClinicID = clinic.ClinicID,
                    PatientID = patientId,
                    Date = model.AppointmentDate.Value,
                    Time = model.AppointmentTime.Value,
                    isBooked = true,
                    CreatedByAssistantID = id
                };

                _appointmentRepository.Add(appointment);
                _appointmentRepository.Save();

                var booking = new Booking
                {
                    AppointmentID = appointment.AppointmentID,
                    PatientID = patientId,
                    DoctorID = model.DoctorID.Value,
                    ClinicID = clinic.ClinicID,
                    IsActive = true,
                    Status = "Confirmed",
                    Reason = model.Reason ?? string.Empty,
                    Notes = model.Notes ?? string.Empty
                };

                _bookingRepository.Add(booking);
                _bookingRepository.Save();

                var apptDoctorName = doctorSummaries.FirstOrDefault(d => d.DoctorID == model.DoctorID.Value)?.FullName ?? "Doctor";
                CreateOperationalAlert(patientId,
                    "Appointment Scheduled",
                    $"Appointment for {model.PatientName} with {apptDoctorName} on {model.AppointmentDate.Value:MMM dd, yyyy} at {model.AppointmentTime.Value:hh\\:mm} has been confirmed.",
                    AlertTypes.Info);

                model.CreatedAppointmentID = appointment.AppointmentID;
                var pwNote = newPatientTempPassword != null
                    ? $" New patient login: {model.NewPatientEmail} / {newPatientTempPassword}"
                    : string.Empty;
                TempData["SuccessMessage"] = $"Appointment created successfully for {model.PatientName} on {model.AppointmentDate:MMM dd, yyyy} at {model.AppointmentTime:hh\\:mm}.{pwNote}";

                return RedirectToAction(nameof(Appointments), new { id = id });
            }
            catch (Exception ex)
            {
                model.ErrorMessage = $"Failed to create appointment: {ex.Message}";
                return View(model);
            }
        }

        public IActionResult Details(int id) => NotFound();

        public IActionResult Create() => NotFound();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Assistant assistant) => NotFound();

        public IActionResult Edit(int id) => NotFound();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(int id, Assistant assistant) => NotFound();

        public IActionResult Delete(int id) => NotFound();

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id) => NotFound();

        private IActionResult? TryResolveAssistant(int id, out Assistant? assistant, bool returnJsonOnFailure = false)
        {
            assistant = null;

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
            {
                if (returnJsonOnFailure)
                    return Unauthorized(new { success = false, message = "Unauthorized." });

                return Unauthorized();
            }

            assistant = _assistantRepository.GetAll()
                .Where(a => a.UserID == userId)
                .Select(a => _assistantRepository.GetByIdWithDoctors(a.AssistantID))
                .FirstOrDefault(a => a != null);

            if (assistant == null)
            {
                if (returnJsonOnFailure)
                    return NotFound(new { success = false, message = "Assistant not found." });

                return NotFound();
            }

            if (id > 0 && assistant.AssistantID != id)
            {
                if (returnJsonOnFailure)
                    return StatusCode(StatusCodes.Status403Forbidden, new { success = false, message = "Access denied." });

                return Forbid();
            }

            return null;
        }

        private IActionResult? TryResolveAssistantClinic(
            int id,
            out Assistant assistant,
            out Clinic clinic,
            bool returnJsonOnFailure = false)
        {
            assistant = null!;
            clinic = null!;

            var assistantResult = TryResolveAssistant(id, out var resolvedAssistant, returnJsonOnFailure);
            if (assistantResult != null)
                return assistantResult;

            assistant = resolvedAssistant!;

            if (assistant.ClinicID is not int clinicId)
            {
                if (returnJsonOnFailure)
                    return NotFound(new { success = false, message = "Assistant is not assigned to a clinic." });

                return NotFound();
            }

            var resolvedClinic = _clinicRepository.GetByIdWithDoctor(clinicId);
            if (resolvedClinic == null)
            {
                if (returnJsonOnFailure)
                    return NotFound(new { success = false, message = "Clinic not found." });

                return NotFound();
            }

            clinic = resolvedClinic;
            return null;
        }

        

        public IActionResult ClinicInvitations(int id)
        {
            var accessResult = TryResolveAssistant(id, out var assistant);
            if (accessResult != null) return accessResult;

            var pending = _context.ClinicInvitations
                .Include(ci => ci.Doctor).ThenInclude(d => d.User)
                .Include(ci => ci.Clinic)
                .Where(ci => ci.AssistantID == assistant!.AssistantID && ci.Status == "Pending")
                .OrderByDescending(ci => ci.SentAtUtc)
                .Select(ci => new AssistantClinicInvitationItemViewModel
                {
                    ClinicInvitationID = ci.ClinicInvitationID,
                    DoctorID = ci.DoctorID,
                    DoctorName = $"Dr. {(ci.Doctor.User.FirstName ?? string.Empty)} {(ci.Doctor.User.LastName ?? string.Empty)}".Trim(),
                    DoctorSpecialization = ci.Doctor.Specialization,
                    ClinicID = ci.ClinicID,
                    ClinicName = ci.Clinic.Name,
                    ClinicLocation = ci.Clinic.Location,
                    SentAtUtc = ci.SentAtUtc
                })
                .ToList();

            var recent = _context.ClinicInvitations
                .Include(ci => ci.Doctor).ThenInclude(d => d.User)
                .Include(ci => ci.Clinic)
                .Where(ci => ci.AssistantID == assistant.AssistantID && ci.Status != "Pending")
                .OrderByDescending(ci => ci.RespondedAtUtc ?? ci.SentAtUtc)
                .Take(12)
                .ToList();

            var vm = new AssistantClinicInvitationsPageViewModel
            {
                Assistant = assistant,
                AssistantName = assistant.User != null
                    ? $"{assistant.User.FirstName} {assistant.User.LastName}".Trim()
                    : "Assistant",
                PendingInvitations = pending,
                RecentInvitations = recent
            };

            ViewData["Title"] = "Clinic Invitations";
            ViewData["AssistantId"] = assistant.AssistantID;
            ViewData["AssistantName"] = vm.AssistantName;
            ViewData["ActivePage"] = "ClinicInvitations";
            ViewData["PageTitle"] = "Clinic Invitations";
            ViewData["PageSubtitle"] = "Accept or decline doctor clinic-team requests";

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AcceptClinicInvitation(int id, int invitationId)
        {
            var accessResult = TryResolveAssistant(id, out var assistant);
            if (accessResult != null) return accessResult;

            var invitation = _context.ClinicInvitations
                .FirstOrDefault(ci => ci.ClinicInvitationID == invitationId && ci.AssistantID == assistant!.AssistantID);

            if (invitation == null)
            {
                TempData["InviteError"] = "Invitation not found.";
                return RedirectToAction(nameof(ClinicInvitations), new { id = assistant!.AssistantID });
            }

            if (!string.Equals(invitation.Status, "Pending", StringComparison.OrdinalIgnoreCase))
            {
                TempData["InviteError"] = "Invitation already processed.";
                return RedirectToAction(nameof(ClinicInvitations), new { id = assistant.AssistantID });
            }

            var trackedAssistant = _context.Assistants.FirstOrDefault(a => a.AssistantID == assistant.AssistantID);
            if (trackedAssistant == null)
            {
                TempData["InviteError"] = "Assistant account not found.";
                return RedirectToAction(nameof(ClinicInvitations), new { id = assistant.AssistantID });
            }

            bool isSwitchingClinic = trackedAssistant.ClinicID.HasValue
                                  && trackedAssistant.ClinicID.Value != invitation.ClinicID;

            var assistantUser = _context.Users.FirstOrDefault(u => u.Id == trackedAssistant.UserID);
            var assistantName = assistantUser != null
                ? $"{assistantUser.FirstName} {assistantUser.LastName}".Trim()
                : invitation.AssistantEmail;

            if (isSwitchingClinic)
            {
                // ── Clinic switch requires approval ──────────────────────────────
                // Don't let her stack a second clinic change while one is in flight.
                bool hasPendingLeave = _context.AssistantLeaveRequests
                    .Any(r => r.AssistantID == trackedAssistant.AssistantID && r.Status == "Pending");
                if (hasPendingLeave)
                {
                    TempData["InviteError"] = "You already have a clinic change awaiting approval. Resolve it before accepting another invitation.";
                    return RedirectToAction(nameof(ClinicInvitations), new { id = assistant.AssistantID });
                }

                // Every doctor she is linked to within her CURRENT clinic must approve.
                int oldClinicId = trackedAssistant.ClinicID!.Value;
                var approverDoctorIds = (from ad in _context.AssistantDoctors
                                         join cd in _context.ClinicDoctors on ad.DoctorID equals cd.DoctorID
                                         where ad.AssistantID == trackedAssistant.AssistantID
                                            && cd.ClinicID == oldClinicId
                                         select ad.DoctorID)
                                        .Distinct()
                                        .ToList();

                if (approverDoctorIds.Count > 0)
                {
                    var leaveRequest = new AssistantLeaveRequest
                    {
                        AssistantID = trackedAssistant.AssistantID,
                        OldClinicID = oldClinicId,
                        NewClinicID = invitation.ClinicID,
                        NewDoctorID = invitation.DoctorID,
                        ClinicInvitationID = invitation.ClinicInvitationID,
                        Status = "Pending",
                        CreatedAtUtc = DateTime.UtcNow
                    };
                    foreach (var docId in approverDoctorIds)
                    {
                        leaveRequest.Approvals.Add(new AssistantLeaveApproval
                        {
                            DoctorID = docId,
                            Status = "Pending"
                        });
                    }
                    _context.AssistantLeaveRequests.Add(leaveRequest);

                    // Hold the invitation in an intermediate state so it can't be
                    // re-accepted and the switch side-effects don't fire yet.
                    invitation.Status = "PendingLeaveApproval";
                    invitation.ResponseMessage = "Awaiting leave approval from current clinic doctors";
                    _context.SaveChanges();

                    foreach (var docId in approverDoctorIds)
                    {
                        _ = _doctorNotificationService.NotifyAsync(
                            docId,
                            "Assistant Leave Request",
                            $"{assistantName} has requested to leave your clinic to join another. Your approval is required.",
                            "leave_request",
                            $"/Doctor/Clinics/{docId}");
                    }

                    TempData["InviteSuccess"] = "Your request to switch clinics was submitted and is awaiting approval from every doctor in your current clinic.";
                    return RedirectToAction(nameof(ClinicInvitations), new { id = assistant.AssistantID });
                }

                // No doctors to approve in the old clinic → switch immediately.
            }

            // Immediate path: first-time assignment, same clinic, or a switch with
            // no required approvers.
            ClinicSwitchHelper.ExecuteSwitch(_context, trackedAssistant, invitation, removeOldLinks: isSwitchingClinic);
            _context.SaveChanges();

            _ = _doctorNotificationService.NotifyAsync(
                invitation.DoctorID,
                "Assistant Joined Your Team",
                $"{assistantName} has accepted your clinic invitation and joined your team.",
                "invitation_accepted",
                "/Doctor/ClinicTeam");

            var successMsg = isSwitchingClinic
                ? "Clinic switched. All previous doctor links have been removed. You are now part of the new clinic team."
                : "Invitation accepted. You are now part of the doctor's clinic team.";

            TempData["InviteSuccess"] = successMsg;
            return RedirectToAction(nameof(ClinicInvitations), new { id = assistant.AssistantID });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeclineClinicInvitation(int id, int invitationId)
        {
            var accessResult = TryResolveAssistant(id, out var assistant);
            if (accessResult != null) return accessResult;

            var invitation = _context.ClinicInvitations
                .FirstOrDefault(ci => ci.ClinicInvitationID == invitationId && ci.AssistantID == assistant!.AssistantID);

            if (invitation == null)
            {
                TempData["InviteError"] = "Invitation not found.";
                return RedirectToAction(nameof(ClinicInvitations), new { id = assistant!.AssistantID });
            }

            if (!string.Equals(invitation.Status, "Pending", StringComparison.OrdinalIgnoreCase))
            {
                TempData["InviteError"] = "Invitation already processed.";
                return RedirectToAction(nameof(ClinicInvitations), new { id = assistant.AssistantID });
            }

            invitation.Status = "Declined";
            invitation.RespondedAtUtc = DateTime.UtcNow;
            invitation.ResponseMessage = "Declined by assistant";
            _context.SaveChanges();

            TempData["InviteSuccess"] = "Invitation declined.";
            return RedirectToAction(nameof(ClinicInvitations), new { id = assistant.AssistantID });
        }
    }
}
