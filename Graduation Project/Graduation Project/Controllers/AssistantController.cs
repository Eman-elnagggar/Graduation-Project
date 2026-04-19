using Graduation_Project.Data;
using Graduation_Project.Interfaces;
using Graduation_Project.Data;
using Graduation_Project.Models;
using Graduation_Project.Services;
using Graduation_Project.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Security.Claims;

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
        private readonly AppDbContext _context;
        private readonly IChatMessageCrypto _chatMessageCrypto;

        public AssistantController(
            IAssistant assistantRepository,
            IClinic clinicRepository,
            IAppointment appointmentRepository,
            IBooking bookingRepository,
            IPatientDoctor patientDoctorRepository,
            IAlert alertRepository,
            ILabTest labTestRepository,
            AssistantScheduleService assistantScheduleService,
            AppDbContext context,
            IChatMessageCrypto chatMessageCrypto)
        {
            _assistantRepository = assistantRepository;
            _clinicRepository = clinicRepository;
            _appointmentRepository = appointmentRepository;
            _bookingRepository = bookingRepository;
            _patientDoctorRepository = patientDoctorRepository;
            _alertRepository = alertRepository;
            _labTestRepository = labTestRepository;
            _assistantScheduleService = assistantScheduleService;
            _context = context;
            _chatMessageCrypto = chatMessageCrypto;
        }

        public IActionResult Index(int id, int? doctorId, DateTime? date, string? status)
        {
            // Fast initial load — only 2 DB queries (assistant + clinic)
            var assistant = _assistantRepository.GetByIdWithDoctors(id);
            if (assistant == null)
                return NotFound();

            var clinic = _clinicRepository.GetByIdWithDoctor(assistant.ClinicID ?? 0);
            if (clinic == null)
                return NotFound();

            var selectedDate = date?.Date ?? DateTime.Today;
            var selectedStatus = NormalizeScheduleStatus(status);

            var relevantDoctorIds = GetRelevantDoctorIds(assistant, clinic);
            var doctorSummaries = BuildDoctorSummaries(assistant, clinic, relevantDoctorIds);

            bool isFiltered = doctorId.HasValue && relevantDoctorIds.Contains(doctorId.Value);
            var selectedDoctorName = isFiltered
                ? doctorSummaries.FirstOrDefault(d => d.DoctorID == doctorId.Value)?.FullName ?? "Doctor"
                : "All Doctors";

            // Return page skeleton — heavy data (stats, schedule) loaded via AJAX
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
                SelectedDoctorName = selectedDoctorName
            };

            return View(viewModel);
        }

        public IActionResult Messages(int id)
        {
            var accessResult = TryResolveAssistant(id, out var assistant);
            if (accessResult != null) return accessResult;

            var clinic = _clinicRepository.GetByIdWithDoctor(assistant.ClinicID);
            if (clinic == null) return NotFound();

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

        [HttpGet]
        public IActionResult ConversationMessages(int id, string userId)
        {
            var accessResult = TryResolveAssistant(id, out var assistant);
            if (accessResult != null) return accessResult;

            var clinic = _clinicRepository.GetByIdWithDoctor(assistant.ClinicID);
            if (clinic == null) return NotFound();

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
                    timestamp = m.SentAtUtc
                })
                .ToList();

            var unreadIncoming = _context.ChatMessages
                .Where(m => m.SenderUserId == receiverUserId
                         && m.ReceiverUserId == assistantUserId
                         && !m.IsRead)
                .ToList();

            if (unreadIncoming.Count > 0)
            {
                var now = DateTime.Now;
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
            var accessResult = TryResolveAssistant(id, out var assistant, true);
            if (accessResult != null) return accessResult;

            var clinic = _clinicRepository.GetByIdWithDoctor(assistant.ClinicID ?? 0);
            if (clinic == null) return NotFound();

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
                ? _context.Alerts
                    .AsNoTracking()
                    .Count(a => uniquePatientIds.Contains(a.PatientID) && !a.IsRead)
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
            var accessResult = TryResolveAssistant(id, out var assistant, true);
            if (accessResult != null) return accessResult;

            var clinic = _clinicRepository.GetByIdWithDoctor(assistant.ClinicID);
            if (clinic == null) return NotFound();

            var relevantDoctorIds = GetRelevantDoctorIds(assistant, clinic);
            var patientIds = _patientDoctorRepository
                .GetApprovedByDoctors(relevantDoctorIds)
                .Select(pd => pd.PatientID)
                .Distinct()
                .ToList();

            var unreadCount = patientIds.Any()
                ? _context.Alerts
                    .AsNoTracking()
                    .Count(a => patientIds.Contains(a.PatientID) && !a.IsRead)
                : 0;

            return Json(new { unreadCount });
        }

        [HttpGet]
        public IActionResult GetScheduleByDate(int id, int? doctorId, string? date, string? status)
        {
            var accessResult = TryResolveAssistant(id, out var assistant, true);
            if (accessResult != null) return accessResult;

            var clinic = _clinicRepository.GetByIdWithDoctor(assistant.ClinicID ?? 0);
            if (clinic == null) return NotFound();

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
            var assistantDoctorIds = assistant.AssistantDoctors?
                .Select(ad => ad.DoctorID).ToHashSet() ?? new HashSet<int>();
            var clinicDoctorIds = clinic.ClinicDoctors?
                .Select(cd => cd.DoctorID).ToHashSet() ?? new HashSet<int>();
            var relevantDoctorIds = assistantDoctorIds.Intersect(clinicDoctorIds).ToList();
            if (!relevantDoctorIds.Any())
                relevantDoctorIds = clinicDoctorIds.ToList();
            return relevantDoctorIds;
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

        public IActionResult Appointments(int id, int? doctorId, DateTime? date)
        {
            var accessResult = TryResolveAssistant(id, out var assistant);
            if (accessResult != null) return accessResult;

            var clinic = _clinicRepository.GetByIdWithDoctor(assistant.ClinicID ?? 0);
            if (clinic == null) return NotFound();

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
            var targetDate = ParseDashboardDate(date);
            var scope = _assistantScheduleService.BuildScope(id, doctorId);
            if (scope == null) return NotFound();

            var counts = _assistantScheduleService.GetCounts(scope, targetDate);

            return Json(new
            {
                confirmed = counts.Confirmed,
                modified = counts.Modified,
                cancelled = counts.Cancelled,
                total = counts.Total
            });
        }

        [HttpGet]
        public IActionResult GetAppointmentDetail(int id, int appointmentId)
        {
            var accessResult = TryResolveAssistant(id, out var assistant, true);
            if (accessResult != null) return accessResult;

            var clinic = _clinicRepository.GetByIdWithDoctor(assistant.ClinicID ?? 0);
            if (clinic == null) return NotFound();

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
            var accessResult = TryResolveAssistant(id, out var assistant, true);
            if (accessResult != null) return accessResult;

            var clinic = _clinicRepository.GetByIdWithDoctor(assistant.ClinicID ?? 0);
            if (clinic == null) return NotFound();

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

            return Json(new { success = true, message = "Appointment modified successfully." });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CancelAppointment(int id, int appointmentId, string reason)
        {
            var accessResult = TryResolveAssistant(id, out var assistant, true);
            if (accessResult != null) return accessResult;

            var clinic = _clinicRepository.GetByIdWithDoctor(assistant.ClinicID ?? 0);
            if (clinic == null) return NotFound();

            var appointment = _appointmentRepository.GetByIdWithBooking(appointmentId);
            if (appointment == null || appointment.ClinicID != clinic.ClinicID)
                return Json(new { success = false, message = "Appointment not found." });

            var relevantDoctorIds = GetRelevantDoctorIds(assistant, clinic);
            if (!relevantDoctorIds.Contains(appointment.DoctorID))
                return Json(new { success = false, message = "Access denied." });

            appointment.isBooked = false;
            _appointmentRepository.Update(appointment);

            if (appointment.Booking != null)
            {
                appointment.Booking.Status = "Cancelled";
                if (!string.IsNullOrWhiteSpace(reason))
                    appointment.Booking.Notes = reason;
                _bookingRepository.Update(appointment.Booking);
            }

            _appointmentRepository.Save();

            return Json(new { success = true, message = "Appointment cancelled successfully." });
        }


        public IActionResult Availability(int id, int? doctorId)
        {
            var accessResult = TryResolveAssistant(id, out var assistant);
            if (accessResult != null) return accessResult;

            var clinic = _clinicRepository.GetByIdWithDoctor(assistant.ClinicID ?? 0);
            if (clinic == null) return NotFound();

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
            var accessResult = TryResolveAssistant(id, out var assistant, true);
            if (accessResult != null) return accessResult;

            var clinic = _clinicRepository.GetByIdWithDoctor(assistant.ClinicID ?? 0);
            if (clinic == null) return NotFound();

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
            var accessResult = TryResolveAssistant(id, out var assistant, true);
            if (accessResult != null) return accessResult;

            var clinic = _clinicRepository.GetByIdWithDoctor(assistant.ClinicID ?? 0);
            if (clinic == null) return NotFound();

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
            var accessResult = TryResolveAssistant(id, out var assistant, true);
            if (accessResult != null) return accessResult;

            var clinic = _clinicRepository.GetByIdWithDoctor(assistant.ClinicID ?? 0);
            if (clinic == null) return NotFound();

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
            var accessResult = TryResolveAssistant(id, out var assistant, true);
            if (accessResult != null) return accessResult;

            var clinic = _clinicRepository.GetByIdWithDoctor(assistant.ClinicID ?? 0);
            if (clinic == null) return NotFound();

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
            var accessResult = TryResolveAssistant(id, out var assistant, true);
            if (accessResult != null) return accessResult;

            var clinic = _clinicRepository.GetByIdWithDoctor(assistant.ClinicID ?? 0);
            if (clinic == null) return NotFound();

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
            var accessResult = TryResolveAssistant(id, out var assistant, true);
            if (accessResult != null) return accessResult;

            var clinic = _clinicRepository.GetByIdWithDoctor(assistant.ClinicID ?? 0);
            if (clinic == null) return NotFound();

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


        public IActionResult Details(int id)
        {
            throw new NotImplementedException();
        }

        public IActionResult Create()
        {
            throw new NotImplementedException();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Assistant assistant)
        {
            throw new NotImplementedException();
        }

        public IActionResult Edit(int id)
        {
            throw new NotImplementedException();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(int id, Assistant assistant)
        {
            throw new NotImplementedException();
        }

        public IActionResult Delete(int id)
        {
            throw new NotImplementedException();
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
        {
            throw new NotImplementedException();
        }

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

            // Update tracked assistant entity to ensure clinic assignment is persisted.
            var trackedAssistant = _context.Assistants.FirstOrDefault(a => a.AssistantID == assistant.AssistantID);
            if (trackedAssistant == null)
            {
                TempData["InviteError"] = "Assistant account not found.";
                return RedirectToAction(nameof(ClinicInvitations), new { id = assistant.AssistantID });
            }

            trackedAssistant.ClinicID = invitation.ClinicID;

            var alreadyLinked = _context.AssistantDoctors.Any(ad =>
                ad.DoctorID == invitation.DoctorID && ad.AssistantID == assistant.AssistantID);

            if (!alreadyLinked)
            {
                _context.AssistantDoctors.Add(new AssistantDoctor
                {
                    DoctorID = invitation.DoctorID,
                    AssistantID = assistant.AssistantID
                });
            }

            invitation.Status = "Accepted";
            invitation.RespondedAtUtc = DateTime.UtcNow;
            invitation.ResponseMessage = "Accepted by assistant";
            _context.SaveChanges();

            TempData["InviteSuccess"] = "Invitation accepted. You are now part of the doctor’s clinic team.";
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
