using Graduation_Project.Data;
using Graduation_Project.Helpers;
using Graduation_Project.Interfaces;
using Graduation_Project.Models;
using Graduation_Project.Services;
using Graduation_Project.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Graduation_Project.Controllers
{
    [Authorize(Roles = "Doctor")]
    public class DoctorController : Controller
    {
        private readonly IAppointment _appointmentRepository;
        private readonly IPatientDoctor _patientDoctorRepository;
        private readonly AppDbContext _context;
        private readonly IAnalysisService _analysisService;
        private readonly IChatMessageCrypto _chatMessageCrypto;
        private readonly MedicationService _medicationService;
        private readonly MedicationAdherenceService _medicationAdherenceService;
        private readonly IWebHostEnvironment _env;
        private readonly IDoctorNotificationService _doctorNotificationService;
        private readonly IPatientNotificationService _patientNotificationService;
        private readonly IPushNotificationService _push;

        public DoctorController(
            IAppointment appointmentRepository,
            IPatientDoctor patientDoctorRepository,
            AppDbContext context,
            IAnalysisService analysisService,
            IChatMessageCrypto chatMessageCrypto,
            MedicationService medicationService,
            MedicationAdherenceService medicationAdherenceService,
            IWebHostEnvironment env,
            IDoctorNotificationService doctorNotificationService,
            IPatientNotificationService patientNotificationService,
            IPushNotificationService push)
        {
            _appointmentRepository = appointmentRepository;
            _patientDoctorRepository = patientDoctorRepository;
            _context = context;
            _analysisService = analysisService;
            _chatMessageCrypto = chatMessageCrypto;
            _medicationService = medicationService;
            _medicationAdherenceService = medicationAdherenceService;
            _env = env;
            _doctorNotificationService = doctorNotificationService;
            _patientNotificationService = patientNotificationService;
            _push = push;
        }

        [HttpGet]
        public async Task<IActionResult> UnderReview()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var doctor = await _context.Doctors
                .Include(d => d.User)
                .FirstOrDefaultAsync(d => d.UserID == userId);

            if (doctor == null) return NotFound();

            if (doctor.VerificationStatus == "Approved")
                return RedirectToAction("Index", "Doctor");

            ViewData["Title"] = "Account Under Review";
            ViewData["DoctorName"] = $"{doctor.User?.FirstName} {doctor.User?.LastName}".Trim();
            ViewData["VerificationStatus"] = doctor.VerificationStatus ?? "Pending";
            ViewData["RejectionNote"] = doctor.RejectionNote;
            return View();
        }

        public IActionResult Index(int id = 0)
        {
            var accessResult = TryResolveDoctor(id, out var doctor);
            if (accessResult != null)
                return accessResult;

            var doctorName = BuildDoctorName(doctor);
            var today = DateTime.Today;
            var endOfWeek = today.AddDays(7);

            var appointments = _context.Appointments
                .Include(a => a.Patient).ThenInclude(p => p.User)
                .Include(a => a.Bookings)
                .Where(a => a.DoctorID == doctor.DoctorID)
                .ToList();

            var todayAppointments = appointments
                .Where(a => a.Date.Date == today && a.isBooked)
                .OrderBy(a => a.Time)
                .ToList();

            var thisWeekAppointmentsCount = appointments.Count(a =>
                a.isBooked && a.Date.Date >= today && a.Date.Date < endOfWeek);

            var nextAppointment = appointments
                .Where(a => a.isBooked && a.Date.Date >= today)
                .OrderBy(a => a.Date)
                .ThenBy(a => a.Time)
                .FirstOrDefault();

            var approvedPatients = _patientDoctorRepository
                .GetApprovedByDoctor(doctor.DoctorID)
                .Select(pd => pd.Patient)
                .Where(p => p != null)
                .ToList();

            var patientSummaries = BuildPatientSummaries(doctor.DoctorID, approvedPatients);
            var highRiskCount = patientSummaries.Count(p =>
                string.Equals(p.RiskLevel, "high", StringComparison.OrdinalIgnoreCase));
            var mediumRiskCount = patientSummaries.Count(p =>
                string.Equals(p.RiskLevel, "medium", StringComparison.OrdinalIgnoreCase));
            var lowRiskCount = patientSummaries.Count(p =>
                string.Equals(p.RiskLevel, "low", StringComparison.OrdinalIgnoreCase));

            var weekStart = today.AddDays(-(int)today.DayOfWeek);
            var weeklyCounts = Enumerable.Range(0, 7)
                .Select(i =>
                {
                    var day = weekStart.AddDays(i).Date;
                    return appointments.Count(a => a.isBooked && a.Date.Date == day);
                })
                .ToList();

            var approvedPatientIds = approvedPatients.Select(p => p.PatientID).ToList();
            var recentAlerts = _context.Alerts
                .Include(a => a.Patient)
                .Where(a => a.Patient != null
                         && approvedPatientIds.Contains(a.PatientID))
                .OrderByDescending(a => a.DateCreated)
                .Take(5)
                .ToList();

            var unreadAlertsCount = _context.Alerts
                .Count(a => approvedPatientIds.Contains(a.PatientID) && !a.IsRead);

            var recentNotifications = _context.DoctorNotifications
                .Where(n => n.DoctorID == doctor.DoctorID)
                .OrderByDescending(n => n.DateCreated)
                .Take(5)
                .ToList();

            var unreadNotificationsCount = _context.DoctorNotifications
                .Count(n => n.DoctorID == doctor.DoctorID && !n.IsRead);

            var doctorUserId = doctor.UserID;
            var patientUserIdToPatient = approvedPatients
                .Where(p => !string.IsNullOrWhiteSpace(p.UserID))
                .GroupBy(p => p.UserID!)
                .ToDictionary(g => g.Key, g => g.First());

            var patientUserIds = patientUserIdToPatient.Keys.ToList();

            var chatMessages = patientUserIds.Count == 0
                ? new List<ChatMessage>()
                : _context.ChatMessages
                    .Where(m => (m.SenderUserId == doctorUserId && patientUserIds.Contains(m.ReceiverUserId))
                             || (m.ReceiverUserId == doctorUserId && patientUserIds.Contains(m.SenderUserId)))
                    .OrderByDescending(m => m.SentAtUtc)
                    .ToList();

            var recentMessages = patientUserIds
                .Select(patientUserId =>
                {
                    var patient = patientUserIdToPatient[patientUserId];
                    var conversationMessages = chatMessages
                        .Where(m => m.SenderUserId == patientUserId || m.ReceiverUserId == patientUserId)
                        .ToList();

                    var latest = conversationMessages.FirstOrDefault();

                    return new DoctorDashboardRecentMessageSummary
                    {
                        PatientId = patient.PatientID,
                        PatientName = BuildPatientName(patient),
                        LastMessagePreview = latest != null ? _chatMessageCrypto.Decrypt(latest.Message) : "Start a conversation",
                        LastMessageTime = latest?.SentAtUtc,
                        UnreadCount = conversationMessages.Count(m => m.SenderUserId == patientUserId && m.ReceiverUserId == doctorUserId && !m.IsRead)
                    };
                })
                .OrderByDescending(m => m.LastMessageTime ?? DateTime.MinValue)
                .Take(4)
                .ToList();

            var unreadMessagesCount = recentMessages.Sum(m => m.UnreadCount);

            var vm = new DoctorDashboardViewModel
            {
                Doctor = doctor,
                DoctorName = doctorName,
                TodayAppointmentsCount = todayAppointments.Count,
                ThisWeekAppointmentsCount = thisWeekAppointmentsCount,
                ActivePatientsCount = approvedPatients.Count,
                NewPatientsThisMonth = _patientDoctorRepository
                    .GetApprovedByDoctor(doctor.DoctorID)
                    .Count(pd => pd.ResponseDate.HasValue
                              && pd.ResponseDate.Value.Year == today.Year
                              && pd.ResponseDate.Value.Month == today.Month),
                HighRiskPatientsCount = highRiskCount,
                MediumRiskPatientsCount = mediumRiskCount,
                LowRiskPatientsCount = lowRiskCount,
                UnreadMessagesCount = unreadMessagesCount,
                UrgentMessagesCount = 0,
                UnreadAlertsCount = unreadAlertsCount,
                UnreadNotificationsCount = unreadNotificationsCount,
                WeeklyAppointmentCounts = weeklyCounts,
                NextAppointment = nextAppointment,
                TodayAppointments = todayAppointments,
                RecentAlerts = recentAlerts,
                RecentNotifications = recentNotifications,
                RecentMessages = recentMessages,
                PriorityPatients = patientSummaries
                    .Where(p => p.NeedsAttention)
                    .Take(6)
                    .ToList()
            };

            return View(vm);
        }

        [HttpGet]
        public IActionResult PatientMedicationSummary(int id, int patientId)
        {
            var accessResult = TryResolveDoctor(id, out var doctor);
            if (accessResult != null)
                return accessResult;

            var isAssigned = _patientDoctorRepository
                .GetApprovedByDoctor(doctor!.DoctorID)
                .Any(pd => pd.PatientID == patientId);
            if (!isAssigned)
                return Forbid();

            var patient = _context.Patients
                .Include(p => p.User)
                .FirstOrDefault(p => p.PatientID == patientId);
            if (patient == null)
                return NotFound();

            var startDate = DateTime.Today.AddDays(-30);
            var endDate = DateTime.Today.AddDays(1);
            var summary = _medicationAdherenceService.GetSummary(patientId, startDate, endDate);
            var recentLogs = _context.MedicationLogs
                .Include(l => l.Medication)
                .Where(l => l.Medication.PatientID == patientId)
                .OrderByDescending(l => l.ScheduledAt)
                .Take(10)
                .ToList();

            var doctorName = BuildDoctorName(doctor);
            var patientName = BuildPatientName(patient);

            var viewModel = new DoctorMedicationSummaryViewModel
            {
                Doctor = doctor,
                Patient = patient,
                DoctorName = doctorName,
                PatientName = patientName,
                Summary = summary,
                RecentLogs = recentLogs
            };

            return View("~/Views/Doctor/PatientMedicationSummary.cshtml", viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadChatFile(int id, IFormFile file)
        {
            var accessResult = TryResolveDoctor(id, out var doctor);
            if (accessResult != null)
                return accessResult;

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

            var userDir = Path.Combine(_env.WebRootPath, "uploads", "chat", doctor!.UserID!);
            Directory.CreateDirectory(userDir);

            var ext = Path.GetExtension(file.FileName);
            var fileName = $"{Guid.NewGuid():N}{ext}";
            var filePath = Path.Combine(userDir, fileName);

            using (var stream = System.IO.File.Create(filePath))
                await file.CopyToAsync(stream);

            var url = $"/uploads/chat/{doctor.UserID}/{fileName}";
            var type = file.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase) ? "image" : "file";

            return Json(new { url, type, name = file.FileName });
        }

        [HttpGet]
        public IActionResult ConversationMessages(int id, string userId)
        {
            var accessResult = TryResolveDoctor(id, out var doctor);
            if (accessResult != null)
                return accessResult;

            var approvedPatientUserIds = _patientDoctorRepository
                .GetApprovedByDoctor(doctor.DoctorID)
                .Select(pd => pd.Patient)
                .Where(p => p != null && !string.IsNullOrWhiteSpace(p.UserID))
                .Select(p => p!.UserID!)
                .Distinct()
                .ToList();

            var linkedAssistantUserIds = _context.AssistantDoctors
                .Where(ad => ad.DoctorID == doctor.DoctorID)
                .Include(ad => ad.Assistant)
                .Where(ad => ad.Assistant != null && !string.IsNullOrWhiteSpace(ad.Assistant!.UserID))
                .Select(ad => ad.Assistant!.UserID!)
                .Distinct()
                .ToList();

            var linkedUserIds = approvedPatientUserIds
                .Concat(linkedAssistantUserIds)
                .Distinct()
                .ToList();

            if (string.IsNullOrWhiteSpace(userId) || !linkedUserIds.Contains(userId))
                return Forbid();

            var doctorUserId = doctor.UserID;
            var receiverUserId = userId;

            var messages = _context.ChatMessages
                .Where(m => (m.SenderUserId == doctorUserId && m.ReceiverUserId == receiverUserId)
                         || (m.SenderUserId == receiverUserId && m.ReceiverUserId == doctorUserId))
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
                         && m.ReceiverUserId == doctorUserId
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UpdateAppointmentStatus(int id, int appointmentId, string status)
        {
            var accessResult = TryResolveDoctor(id, out var doctor, true);
            if (accessResult != null)
                return accessResult;

            var appointment = _context.Appointments
                .Include(a => a.Bookings)
                .FirstOrDefault(a => a.AppointmentID == appointmentId && a.DoctorID == doctor.DoctorID);

            if (appointment == null || !appointment.isBooked || !appointment.PatientID.HasValue)
                return Json(new { success = false, message = "Booked appointment not found." });

            var normalizedStatus = NormalizeDoctorBookingStatus(status);
            if (normalizedStatus == null)
                return Json(new { success = false, message = "Invalid status value." });

            if (appointment.Booking == null)
            {
                appointment.Booking = new Booking
                {
                    AppointmentID = appointment.AppointmentID,
                    PatientID = appointment.PatientID.Value,
                    DoctorID = appointment.DoctorID,
                    ClinicID = appointment.ClinicID,
                    IsActive = true,
                    Status = normalizedStatus,
                    Reason = string.Empty,
                    Notes = string.Empty
                };
                _context.Bookings.Add(appointment.Booking);
            }
            else
            {
                appointment.Booking.Status = normalizedStatus;
            }

            var autoMissedIds = new List<int>();
            var addedToMyDoctors = false;
            if (string.Equals(normalizedStatus, "Completed", StringComparison.OrdinalIgnoreCase))
            {
                var patientId = appointment.PatientID.Value;
                var now = DateTime.Now;

                var existingDoctorLink = _patientDoctorRepository.GetById(doctor.DoctorID, patientId);
                if (existingDoctorLink == null)
                {
                    var patientHasPrimaryDoctor = _context.PatientDoctors
                        .AsNoTracking()
                        .Any(pd => pd.PatientID == patientId
                                && pd.Status == "Approved"
                                && pd.IsPrimary);

                    _patientDoctorRepository.Add(new PatientDoctor
                    {
                        DoctorID = doctor.DoctorID,
                        PatientID = patientId,
                        Status = "Approved",
                        RequestDate = now,
                        ResponseDate = now,
                        IsPrimary = !patientHasPrimaryDoctor
                    });

                    addedToMyDoctors = true;
                }
                else if (!string.Equals(existingDoctorLink.Status, "Approved", StringComparison.OrdinalIgnoreCase))
                {
                    existingDoctorLink.Status = "Approved";
                    existingDoctorLink.ResponseDate ??= now;

                    var patientHasDifferentPrimaryDoctor = _context.PatientDoctors
                        .AsNoTracking()
                        .Any(pd => pd.PatientID == patientId
                                && pd.Status == "Approved"
                                && pd.IsPrimary
                                && !(pd.DoctorID == doctor.DoctorID && pd.PatientID == patientId));

                    if (!patientHasDifferentPrimaryDoctor)
                        existingDoctorLink.IsPrimary = true;

                    _patientDoctorRepository.Update(existingDoctorLink);
                    addedToMyDoctors = true;
                }

                var autoMissedCandidates = _context.Appointments
                    .Include(a => a.Bookings)
                    .Where(a => a.DoctorID == doctor.DoctorID
                             && a.AppointmentID != appointment.AppointmentID
                             && a.isBooked
                             && a.PatientID.HasValue
                             && a.Bookings.Any(b => b.IsActive))
                    .ToList()
                    .Where(a => a.Date.Date.Add(a.Time).AddHours(1) < now
                             && !string.Equals(a.Booking!.Status, "Completed", StringComparison.OrdinalIgnoreCase)
                             && !string.Equals(a.Booking!.Status, "Cancelled", StringComparison.OrdinalIgnoreCase)
                             && !string.Equals(a.Booking!.Status, "Missed", StringComparison.OrdinalIgnoreCase))
                    .ToList();

                foreach (var candidate in autoMissedCandidates)
                {
                    candidate.Booking!.Status = "Missed";
                    autoMissedIds.Add(candidate.AppointmentID);
                }
            }

            _context.SaveChanges();

            return Json(new
            {
                success = true,
                appointmentId = appointment.AppointmentID,
                status = normalizedStatus.ToLowerInvariant(),
                addedToMyDoctors,
                autoMissedIds,
                message = autoMissedIds.Count > 0
                    ? "Status updated. Past unfinished appointments were marked as missed."
                    : "Status updated successfully."
            });
        }

        public IActionResult Patients(int id = 0)
        {
            var accessResult = TryResolveDoctor(id, out var doctor);
            if (accessResult != null)
                return accessResult;

            var approvedPatients = _patientDoctorRepository
                .GetApprovedByDoctor(doctor.DoctorID)
                .Select(pd => pd.Patient)
                .Where(p => p != null)
                .ToList();

            var vm = new DoctorPatientsViewModel
            {
                Doctor = doctor,
                DoctorName = BuildDoctorName(doctor),
                Patients = BuildPatientSummaries(doctor.DoctorID, approvedPatients)
            };

            return View(vm);
        }

        public IActionResult Messages(int id = 0)
        {
            var accessResult = TryResolveDoctor(id, out var doctor);
            if (accessResult != null)
                return accessResult;

            var approvedPatients = _patientDoctorRepository
                .GetApprovedByDoctor(doctor.DoctorID)
                .Select(pd => pd.Patient)
                .Where(p => p != null)
                .ToList();

            var doctorUserId = doctor.UserID;
            var patientUserIds = approvedPatients
                .Select(p => p.UserID)
                .Where(u => !string.IsNullOrWhiteSpace(u))
                .ToList();

            var linkedAssistants = _context.AssistantDoctors
                .Where(ad => ad.DoctorID == doctor.DoctorID)
                .Include(ad => ad.Assistant)
                    .ThenInclude(a => a.User)
                .Where(ad => ad.Assistant != null && !string.IsNullOrWhiteSpace(ad.Assistant!.UserID))
                .Select(ad => ad.Assistant!)
                .GroupBy(a => a.AssistantID)
                .Select(g => g.First())
                .ToList();

            var assistantUserIds = linkedAssistants
                .Select(a => a.UserID)
                .Where(u => !string.IsNullOrWhiteSpace(u))
                .ToList();

            var receiverUserIds = patientUserIds
                .Concat(assistantUserIds)
                .Distinct()
                .ToList();

            var chatMessages = _context.ChatMessages
                .Where(m => (m.SenderUserId == doctorUserId && receiverUserIds.Contains(m.ReceiverUserId))
                         || (m.ReceiverUserId == doctorUserId && receiverUserIds.Contains(m.SenderUserId)))
                .OrderByDescending(m => m.SentAtUtc)
                .ToList();

            var patientConversations = approvedPatients
                .Select(p => new
                {
                    participantId = p.PatientID,
                    participantType = "Patient",
                    ReceiverUserId = p.UserID ?? string.Empty,
                    participantName = BuildPatientName(p)
                });

            var assistantConversations = linkedAssistants
                .Select(a => new
                {
                    participantId = a.AssistantID,
                    participantType = "Assistant",
                    ReceiverUserId = a.UserID ?? string.Empty,
                    participantName = a.User != null
                        ? $"{a.User.FirstName} {a.User.LastName}".Trim()
                        : "Assistant"
                });

            var conversations = patientConversations
                .Concat(assistantConversations)
                .Where(c => !string.IsNullOrWhiteSpace(c.ReceiverUserId))
                .GroupBy(c => c.ReceiverUserId)
                .Select(g => g.First())
                .Select(c => new DoctorConversationSummary
                {
                    ParticipantId = c.participantId,
                    ParticipantType = c.participantType,
                    ReceiverUserId = c.ReceiverUserId,
                    ParticipantName = c.participantName,
                    UnreadCount = chatMessages.Count(m => m.SenderUserId == c.ReceiverUserId && m.ReceiverUserId == doctorUserId && !m.IsRead),
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

            var vm = new DoctorMessagesViewModel
            {
                Doctor = doctor,
                DoctorName = BuildDoctorName(doctor),
                Conversations = conversations
            };

            return View(vm);
        }

        public IActionResult Schedule(int id = 0)
        {
            var accessResult = TryResolveDoctor(id, out var doctor);
            if (accessResult != null)
                return accessResult;

            var now = DateTime.Now;
            var appointmentsToAutoMiss = _context.Appointments
                .Include(a => a.Bookings)
                .Where(a => a.DoctorID == doctor.DoctorID
                         && a.isBooked
                         && a.PatientID.HasValue)
                .ToList()
                .Where(a => a.Date.Date.Add(a.Time).AddHours(1) < now)
                .Where(a => a.Booking == null
                         || (!string.Equals(a.Booking.Status, "Completed", StringComparison.OrdinalIgnoreCase)
                             && !string.Equals(a.Booking.Status, "Cancelled", StringComparison.OrdinalIgnoreCase)
                             && !string.Equals(a.Booking.Status, "Missed", StringComparison.OrdinalIgnoreCase)))
                .ToList();

            if (appointmentsToAutoMiss.Count > 0)
            {
                foreach (var appointment in appointmentsToAutoMiss)
                {
                    if (appointment.Booking == null)
                    {
                        appointment.Booking = new Booking
                        {
                            AppointmentID = appointment.AppointmentID,
                            PatientID = appointment.PatientID.Value,
                            DoctorID = appointment.DoctorID,
                            ClinicID = appointment.ClinicID,
                            IsActive = true,
                            Status = "Missed",
                            Reason = string.Empty,
                            Notes = string.Empty
                        };
                        _context.Bookings.Add(appointment.Booking);
                    }
                    else
                    {
                        appointment.Booking.Status = "Missed";
                    }
                }

                _context.SaveChanges();
            }

            var appointments = _context.Appointments
                .Include(a => a.Patient).ThenInclude(p => p.User)
                .Include(a => a.Bookings)
                .Where(a => a.DoctorID == doctor.DoctorID)
                .OrderBy(a => a.Date)
                .ThenBy(a => a.Time)
                .Take(250)
                .ToList();

            var vm = new DoctorScheduleViewModel
            {
                Doctor = doctor,
                DoctorName = BuildDoctorName(doctor),
                Appointments = appointments
            };

            return View(vm);
        }

        // Clinic Team is now merged into the Clinics page; keep this route for backwards compatibility.
        public IActionResult ClinicTeam(int id = 0)
        {
            var accessResult = TryResolveDoctor(id, out var doctor);
            if (accessResult != null)
                return accessResult;

            return RedirectToAction(nameof(Clinics), new { id = doctor!.DoctorID });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult InviteAssistant(int doctorId, string assistantEmail, int clinicId)
        {
            var accessResult = TryResolveDoctor(doctorId, out var doctor);
            if (accessResult != null)
                return accessResult;

            if (string.IsNullOrWhiteSpace(assistantEmail))
            {
                TempData["InviteError"] = "Assistant email is required.";
                return RedirectToAction(nameof(Clinics), new { id = doctor!.DoctorID });
            }

            var clinic = _context.ClinicDoctors
                .Include(cd => cd.Clinic)
                .Where(cd => cd.DoctorID == doctor!.DoctorID && cd.ClinicID == clinicId)
                .Select(cd => cd.Clinic)
                .FirstOrDefault();

            if (clinic == null)
            {
                TempData["InviteError"] = "Please select a valid clinic linked to your account.";
                return RedirectToAction(nameof(Clinics), new { id = doctor.DoctorID });
            }

            var normalizedEmail = assistantEmail.Trim().ToLowerInvariant();

            var assistant = _context.Assistants
                .Include(a => a.User)
                .FirstOrDefault(a => a.User.Email != null && a.User.Email.ToLower() == normalizedEmail);

            if (assistant == null)
            {
                TempData["InviteError"] = "No assistant account found with this email.";
                return RedirectToAction(nameof(Clinics), new { id = doctor.DoctorID });
            }

            // Removed hard restriction: assistant can be invited even when not currently assigned to the selected clinic.

            var exists = _context.AssistantDoctors.Any(ad =>
                ad.DoctorID == doctor.DoctorID && ad.AssistantID == assistant.AssistantID);
            if (exists)
            {
                TempData["InviteError"] = "Assistant is already linked to your clinic team.";
                return RedirectToAction(nameof(Clinics), new { id = doctor.DoctorID });
            }

            var pendingExists = _context.ClinicInvitations.Any(ci =>
                ci.DoctorID == doctor.DoctorID
                && ci.ClinicID == clinicId
                && ci.AssistantID == assistant.AssistantID
                && ci.Status == "Pending");

            if (pendingExists)
            {
                TempData["InviteError"] = "A pending invitation already exists for this assistant in the selected clinic.";
                return RedirectToAction(nameof(Clinics), new { id = doctor.DoctorID });
            }

            _context.ClinicInvitations.Add(new ClinicInvitation
            {
                DoctorID = doctor.DoctorID,
                ClinicID = clinicId,
                AssistantID = assistant.AssistantID,
                AssistantEmail = assistant.User.Email ?? normalizedEmail,
                Status = "Pending",
                SentAtUtc = DateTime.UtcNow
            });
            _context.SaveChanges();

            TempData["InviteSuccess"] = "Invitation sent successfully. Assistant must accept it first.";
            return RedirectToAction(nameof(Clinics), new { id = doctor.DoctorID });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RemoveAssistant(int doctorId, int assistantId)
        {
            var accessResult = TryResolveDoctor(doctorId, out var doctor);
            if (accessResult != null)
                return accessResult;

            var doctorClinicIds = _context.ClinicDoctors
                .Where(cd => cd.DoctorID == doctor!.DoctorID)
                .Select(cd => cd.ClinicID)
                .ToList();

            var assistant = _context.Assistants.FirstOrDefault(a => a.AssistantID == assistantId);
            if (assistant == null
                || !assistant.ClinicID.HasValue
                || !doctorClinicIds.Contains(assistant.ClinicID.Value))
            {
                TempData["InviteError"] = "Assistant not found in your linked clinics.";
                return RedirectToAction(nameof(Clinics), new { id = doctor.DoctorID });
            }

            var link = _context.AssistantDoctors
                .FirstOrDefault(ad => ad.DoctorID == doctor.DoctorID && ad.AssistantID == assistantId);

            if (link != null)
            {
                _context.AssistantDoctors.Remove(link);
                _context.SaveChanges();
                TempData["InviteSuccess"] = "Assistant removed from your clinic team.";
            }

            return RedirectToAction(nameof(Clinics), new { id = doctor.DoctorID });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CancelInvitation(int invitationId, int id)
        {
            var accessResult = TryResolveDoctor(id, out var doctor);
            if (accessResult != null)
                return accessResult;

            var invitation = _context.ClinicInvitations
                .FirstOrDefault(ci => ci.ClinicInvitationID == invitationId && ci.DoctorID == doctor!.DoctorID);

            if (invitation == null)
            {
                TempData["InviteError"] = "Invitation not found.";
                return RedirectToAction(nameof(Clinics), new { id = doctor.DoctorID });
            }

            if (!string.Equals(invitation.Status, "Pending", StringComparison.OrdinalIgnoreCase))
            {
                TempData["InviteError"] = "Only pending invitations can be cancelled.";
                return RedirectToAction(nameof(Clinics), new { id = doctor.DoctorID });
            }

            invitation.Status = "Cancelled";
            invitation.RespondedAtUtc = DateTime.UtcNow;
            invitation.ResponseMessage = "Cancelled by doctor";
            _context.SaveChanges();

            TempData["InviteSuccess"] = "Invitation cancelled successfully.";
            return RedirectToAction(nameof(Clinics), new { id = doctor.DoctorID });
        }

        public IActionResult Clinics(int id = 0)
        {
            var accessResult = TryResolveDoctor(id, out var doctor);
            if (accessResult != null)
                return accessResult;

            var clinics = _context.Clinics
                .Include(c => c.ClinicDoctors)
                    .ThenInclude(cd => cd.Doctor)
                        .ThenInclude(d => d.User)
                .Include(c => c.Assistants)
                    .ThenInclude(a => a.User)
                .Where(c => c.ClinicDoctors.Any(cd => cd.DoctorID == doctor!.DoctorID))
                .OrderBy(c => c.Name)
                .ToList();

            var linkedClinicIds = clinics
                .Select(c => c.ClinicID)
                .ToHashSet();

            // Clinic Team data (merged into the Clinics page)
            var linkedClinics = _context.ClinicDoctors
                .Where(cd => cd.DoctorID == doctor!.DoctorID)
                .Select(cd => cd.Clinic)
                .OrderBy(c => c.Name)
                .ToList();

            var assistantIds = _context.AssistantDoctors
                .Where(ad => ad.DoctorID == doctor.DoctorID)
                .Select(ad => ad.AssistantID)
                .ToList();

            var assistants = _context.Assistants
                .Include(a => a.User)
                .Include(a => a.Clinic)
                .Where(a => assistantIds.Contains(a.AssistantID)
                         && a.ClinicID.HasValue
                         && linkedClinicIds.Contains(a.ClinicID.Value))
                .OrderBy(a => a.User.FirstName)
                .ToList();

            var pendingInvitations = _context.ClinicInvitations
                .Include(ci => ci.Clinic)
                .Include(ci => ci.Assistant).ThenInclude(a => a.User)
                .Where(ci => ci.DoctorID == doctor.DoctorID && ci.Status == "Pending")
                .OrderByDescending(ci => ci.SentAtUtc)
                .Select(ci => new PendingInvitationViewModel
                {
                    InvitationID = ci.ClinicInvitationID,
                    Email = ci.AssistantEmail,
                    SentAt = ci.SentAtUtc.ToLocalTime(),
                    ClinicID = ci.ClinicID,
                    ClinicName = ci.Clinic.Name,
                    AssistantName = ((ci.Assistant.User.FirstName ?? string.Empty) + " " + (ci.Assistant.User.LastName ?? string.Empty)).Trim()
                })
                .ToList();

            // Leave requests where THIS doctor still owes a Pending approval.
            var leaveRequests = _context.AssistantLeaveApprovals
                .Include(ap => ap.LeaveRequest).ThenInclude(r => r.Assistant).ThenInclude(a => a.User)
                .Include(ap => ap.LeaveRequest).ThenInclude(r => r.OldClinic)
                .Include(ap => ap.LeaveRequest).ThenInclude(r => r.NewClinic)
                .Where(ap => ap.DoctorID == doctor.DoctorID
                          && ap.Status == "Pending"
                          && ap.LeaveRequest.Status == "Pending")
                .OrderByDescending(ap => ap.LeaveRequest.CreatedAtUtc)
                .Select(ap => new DoctorLeaveRequestViewModel
                {
                    LeaveRequestID = ap.AssistantLeaveRequestID,
                    AssistantName = ((ap.LeaveRequest.Assistant.User.FirstName ?? string.Empty)
                                     + " " + (ap.LeaveRequest.Assistant.User.LastName ?? string.Empty)).Trim(),
                    OldClinicName = ap.LeaveRequest.OldClinic.Name,
                    NewClinicName = ap.LeaveRequest.NewClinic.Name,
                    CreatedAt = ap.LeaveRequest.CreatedAtUtc.ToLocalTime(),
                    ApprovedCount = ap.LeaveRequest.Approvals.Count(x => x.Status == "Approved"),
                    TotalApprovers = ap.LeaveRequest.Approvals.Count(),
                    ThisDoctorResponded = false
                })
                .ToList();

            var managedClinics = BuildClinicManagement(clinics, doctor!.DoctorID);

            // Clinic invitations addressed to THIS doctor, awaiting a response.
            var incomingDoctorInvitations = _context.ClinicDoctorInvitations
                .Include(i => i.Clinic)
                .Include(i => i.Inviter).ThenInclude(d => d.User)
                .Where(i => i.InviteeDoctorID == doctor.DoctorID && i.Status == "Pending")
                .OrderByDescending(i => i.SentAtUtc)
                .Select(i => new IncomingClinicDoctorInvitationViewModel
                {
                    InvitationID = i.ClinicDoctorInvitationID,
                    ClinicID = i.ClinicID,
                    ClinicName = i.Clinic.Name,
                    ClinicLocation = i.Clinic.Location,
                    InviterName = ("Dr. " + (i.Inviter.User.FirstName ?? string.Empty) + " " + (i.Inviter.User.LastName ?? string.Empty)).Trim(),
                    SentAt = i.SentAtUtc.ToLocalTime()
                })
                .ToList();

            // Doctor invitations THIS doctor sent that are still pending.
            var pendingDoctorInvitations = _context.ClinicDoctorInvitations
                .Include(i => i.Clinic)
                .Include(i => i.Invitee).ThenInclude(d => d.User)
                .Where(i => i.InviterDoctorID == doctor.DoctorID && i.Status == "Pending")
                .OrderByDescending(i => i.SentAtUtc)
                .Select(i => new PendingDoctorInvitationViewModel
                {
                    InvitationID = i.ClinicDoctorInvitationID,
                    ClinicID = i.ClinicID,
                    ClinicName = i.Clinic.Name,
                    DoctorName = ("Dr. " + (i.Invitee.User.FirstName ?? string.Empty) + " " + (i.Invitee.User.LastName ?? string.Empty)).Trim(),
                    Email = i.InviteeEmail,
                    SentAt = i.SentAtUtc.ToLocalTime()
                })
                .ToList();

            var vm = new DoctorClinicsViewModel
            {
                Doctor = doctor!,
                DoctorName = BuildDoctorName(doctor!),
                Clinics = clinics,
                LinkedClinicIds = linkedClinicIds,
                Assistants = assistants,
                PendingInvitations = pendingInvitations,
                LinkedClinics = linkedClinics,
                LeaveRequests = leaveRequests,
                ManagedClinics = managedClinics,
                IncomingDoctorInvitations = incomingDoctorInvitations,
                PendingDoctorInvitations = pendingDoctorInvitations
            };

            return View(vm);
        }

        // Projects the doctor's clinics into member lists. Only the owner gets
        // CanRemove on anyone, and the owner can never be removed from their own clinic.
        private List<ClinicManagementViewModel> BuildClinicManagement(List<Clinic> clinics, int viewerDoctorId)
        {
            var clinicIds = clinics.Select(c => c.ClinicID).ToList();
            var today = DateTime.Today;

            // Upcoming appointments per (clinic, doctor) — surfaced as a warning before removal.
            var upcomingByDoctor = _context.Appointments
                .Where(a => clinicIds.Contains(a.ClinicID) && a.Date >= today)
                .GroupBy(a => new { a.ClinicID, a.DoctorID })
                .Select(g => new { g.Key.ClinicID, g.Key.DoctorID, Count = g.Count() })
                .ToDictionary(x => (x.ClinicID, x.DoctorID), x => x.Count);

            var result = new List<ClinicManagementViewModel>();

            foreach (var clinic in clinics)
            {
                var isOwner = clinic.OwnerDoctorID == viewerDoctorId;

                var doctors = (clinic.ClinicDoctors ?? new List<ClinicDoctor>())
                    .Where(cd => cd.Doctor?.User != null)
                    .Select(cd =>
                    {
                        var isClinicOwner = clinic.OwnerDoctorID == cd.DoctorID;
                        upcomingByDoctor.TryGetValue((clinic.ClinicID, cd.DoctorID), out var upcoming);

                        return new ClinicMemberViewModel
                        {
                            MemberID = cd.DoctorID,
                            Name = $"Dr. {cd.Doctor.User.FirstName} {cd.Doctor.User.LastName}".Trim(),
                            Email = cd.Doctor.User.Email,
                            Phone = cd.Doctor.User.PhoneNumber,
                            Specialization = cd.Doctor.Specialization,
                            IsOwner = isClinicOwner,
                            IsSelf = cd.DoctorID == viewerDoctorId,
                            // The owner is the admin and cannot be removed from their own clinic.
                            CanRemove = isOwner && !isClinicOwner,
                            UpcomingAppointments = upcoming
                        };
                    })
                    .OrderByDescending(m => m.IsOwner)
                    .ThenBy(m => m.Name)
                    .ToList();

                var clinicAssistants = (clinic.Assistants ?? new List<Assistant>())
                    .Where(a => a.User != null)
                    .Select(a => new ClinicMemberViewModel
                    {
                        MemberID = a.AssistantID,
                        Name = $"{a.User.FirstName} {a.User.LastName}".Trim(),
                        Email = a.User.Email,
                        Phone = a.User.PhoneNumber,
                        CanRemove = isOwner
                    })
                    .OrderBy(m => m.Name)
                    .ToList();

                var owner = doctors.FirstOrDefault(d => d.IsOwner);

                result.Add(new ClinicManagementViewModel
                {
                    ClinicID = clinic.ClinicID,
                    ClinicName = clinic.Name,
                    ClinicLocation = clinic.Location,
                    IsOwner = isOwner,
                    OwnerDoctorID = clinic.OwnerDoctorID,
                    OwnerName = owner?.Name ?? "Unassigned",
                    Doctors = doctors,
                    Assistants = clinicAssistants
                });
            }

            return result;
        }

        // Resolves a clinic only if the given doctor owns it. Returns null otherwise.
        private Clinic? ResolveOwnedClinic(int clinicId, int doctorId) =>
            _context.Clinics.FirstOrDefault(c => c.ClinicID == clinicId && c.OwnerDoctorID == doctorId);

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> InviteDoctorToClinic(int doctorId, int clinicId, string doctorEmail)
        {
            var accessResult = TryResolveDoctor(doctorId, out var doctor);
            if (accessResult != null)
                return accessResult;

            var clinic = ResolveOwnedClinic(clinicId, doctor!.DoctorID);
            if (clinic == null)
            {
                TempData["ClinicError"] = "Only the clinic owner can invite doctors.";
                return RedirectToAction(nameof(Clinics), new { id = doctor.DoctorID });
            }

            if (string.IsNullOrWhiteSpace(doctorEmail))
            {
                TempData["ClinicError"] = "Doctor email is required.";
                return RedirectToAction(nameof(Clinics), new { id = doctor.DoctorID });
            }

            var normalizedEmail = doctorEmail.Trim().ToLowerInvariant();

            var invitee = _context.Doctors
                .Include(d => d.User)
                .FirstOrDefault(d => d.User.Email != null && d.User.Email.ToLower() == normalizedEmail);

            if (invitee == null)
            {
                TempData["ClinicError"] = "No doctor account found with this email.";
                return RedirectToAction(nameof(Clinics), new { id = doctor.DoctorID });
            }

            if (invitee.DoctorID == doctor.DoctorID)
            {
                TempData["ClinicError"] = "You are already a member of this clinic.";
                return RedirectToAction(nameof(Clinics), new { id = doctor.DoctorID });
            }

            var alreadyLinked = _context.ClinicDoctors
                .Any(cd => cd.ClinicID == clinicId && cd.DoctorID == invitee.DoctorID);
            if (alreadyLinked)
            {
                TempData["ClinicError"] = "That doctor is already a member of this clinic.";
                return RedirectToAction(nameof(Clinics), new { id = doctor.DoctorID });
            }

            var pendingExists = _context.ClinicDoctorInvitations
                .Any(i => i.ClinicID == clinicId
                       && i.InviteeDoctorID == invitee.DoctorID
                       && i.Status == "Pending");
            if (pendingExists)
            {
                TempData["ClinicError"] = "That doctor already has a pending invitation to this clinic.";
                return RedirectToAction(nameof(Clinics), new { id = doctor.DoctorID });
            }

            _context.ClinicDoctorInvitations.Add(new ClinicDoctorInvitation
            {
                ClinicID = clinicId,
                InviterDoctorID = doctor.DoctorID,
                InviteeDoctorID = invitee.DoctorID,
                InviteeEmail = invitee.User.Email ?? normalizedEmail,
                Status = "Pending",
                SentAtUtc = DateTime.UtcNow
            });
            _context.SaveChanges();

            await _doctorNotificationService.NotifyAsync(
                invitee.DoctorID,
                "Clinic Invitation",
                $"{BuildDoctorName(doctor)} invited you to join {clinic.Name}.",
                "clinic_invitation",
                $"/Doctor/Clinics/{invitee.DoctorID}");

            TempData["ClinicSuccess"] = "Invitation sent. The doctor must accept it before joining the clinic.";
            return RedirectToAction(nameof(Clinics), new { id = doctor.DoctorID });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CancelDoctorInvitation(int doctorId, int invitationId)
        {
            var accessResult = TryResolveDoctor(doctorId, out var doctor);
            if (accessResult != null)
                return accessResult;

            var invitation = _context.ClinicDoctorInvitations
                .FirstOrDefault(i => i.ClinicDoctorInvitationID == invitationId
                                  && i.InviterDoctorID == doctor!.DoctorID
                                  && i.Status == "Pending");

            if (invitation == null)
            {
                TempData["ClinicError"] = "Pending invitation not found.";
                return RedirectToAction(nameof(Clinics), new { id = doctor!.DoctorID });
            }

            invitation.Status = "Cancelled";
            invitation.RespondedAtUtc = DateTime.UtcNow;
            _context.SaveChanges();

            TempData["ClinicSuccess"] = "Doctor invitation cancelled.";
            return RedirectToAction(nameof(Clinics), new { id = doctor!.DoctorID });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AcceptClinicDoctorInvitation(int id, int invitationId)
        {
            var accessResult = TryResolveDoctor(id, out var doctor);
            if (accessResult != null)
                return accessResult;

            var invitation = _context.ClinicDoctorInvitations
                .Include(i => i.Clinic)
                .FirstOrDefault(i => i.ClinicDoctorInvitationID == invitationId
                                  && i.InviteeDoctorID == doctor!.DoctorID);

            if (invitation == null || !string.Equals(invitation.Status, "Pending", StringComparison.OrdinalIgnoreCase))
            {
                TempData["ClinicError"] = "Invitation not found or already processed.";
                return RedirectToAction(nameof(Clinics), new { id = doctor!.DoctorID });
            }

            invitation.Status = "Accepted";
            invitation.RespondedAtUtc = DateTime.UtcNow;

            var alreadyLinked = _context.ClinicDoctors
                .Any(cd => cd.ClinicID == invitation.ClinicID && cd.DoctorID == doctor!.DoctorID);

            if (!alreadyLinked)
            {
                _context.ClinicDoctors.Add(new ClinicDoctor
                {
                    ClinicID = invitation.ClinicID,
                    DoctorID = doctor!.DoctorID
                });
            }

            _context.SaveChanges();

            await _doctorNotificationService.NotifyAsync(
                invitation.InviterDoctorID,
                "Clinic Invitation Accepted",
                $"{BuildDoctorName(doctor!)} joined {invitation.Clinic.Name}.",
                "invitation_accepted",
                $"/Doctor/Clinics/{invitation.InviterDoctorID}");

            TempData["ClinicSuccess"] = $"You joined {invitation.Clinic.Name}.";
            return RedirectToAction(nameof(Clinics), new { id = doctor!.DoctorID });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeclineClinicDoctorInvitation(int id, int invitationId)
        {
            var accessResult = TryResolveDoctor(id, out var doctor);
            if (accessResult != null)
                return accessResult;

            var invitation = _context.ClinicDoctorInvitations
                .Include(i => i.Clinic)
                .FirstOrDefault(i => i.ClinicDoctorInvitationID == invitationId
                                  && i.InviteeDoctorID == doctor!.DoctorID);

            if (invitation == null || !string.Equals(invitation.Status, "Pending", StringComparison.OrdinalIgnoreCase))
            {
                TempData["ClinicError"] = "Invitation not found or already processed.";
                return RedirectToAction(nameof(Clinics), new { id = doctor!.DoctorID });
            }

            invitation.Status = "Declined";
            invitation.RespondedAtUtc = DateTime.UtcNow;
            _context.SaveChanges();

            await _doctorNotificationService.NotifyAsync(
                invitation.InviterDoctorID,
                "Clinic Invitation Declined",
                $"{BuildDoctorName(doctor!)} declined your invitation to join {invitation.Clinic.Name}.",
                "invitation_declined",
                $"/Doctor/Clinics/{invitation.InviterDoctorID}");

            TempData["ClinicSuccess"] = "Invitation declined.";
            return RedirectToAction(nameof(Clinics), new { id = doctor!.DoctorID });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveDoctorFromClinic(int doctorId, int clinicId, int targetDoctorId)
        {
            var accessResult = TryResolveDoctor(doctorId, out var doctor);
            if (accessResult != null)
                return accessResult;

            var clinic = ResolveOwnedClinic(clinicId, doctor!.DoctorID);
            if (clinic == null)
            {
                TempData["ClinicError"] = "Only the clinic owner can remove doctors.";
                return RedirectToAction(nameof(Clinics), new { id = doctor.DoctorID });
            }

            if (targetDoctorId == clinic.OwnerDoctorID)
            {
                TempData["ClinicError"] = "The clinic owner cannot be removed from their own clinic.";
                return RedirectToAction(nameof(Clinics), new { id = doctor.DoctorID });
            }

            var link = _context.ClinicDoctors
                .FirstOrDefault(cd => cd.ClinicID == clinicId && cd.DoctorID == targetDoctorId);

            if (link == null)
            {
                TempData["ClinicError"] = "That doctor is not a member of this clinic.";
                return RedirectToAction(nameof(Clinics), new { id = doctor.DoctorID });
            }

            _context.ClinicDoctors.Remove(link);

            // Drop the doctor's links to assistants of this clinic — they no longer share one.
            var clinicAssistantIds = _context.Assistants
                .Where(a => a.ClinicID == clinicId)
                .Select(a => a.AssistantID)
                .ToList();

            var assistantLinks = _context.AssistantDoctors
                .Where(ad => ad.DoctorID == targetDoctorId && clinicAssistantIds.Contains(ad.AssistantID))
                .ToList();
            _context.AssistantDoctors.RemoveRange(assistantLinks);

            // Cancel assistant invitations this doctor still has open for this clinic,
            // so nobody can accept an invite from a doctor who has left.
            var openInvites = _context.ClinicInvitations
                .Where(ci => ci.ClinicID == clinicId
                          && ci.DoctorID == targetDoctorId
                          && ci.Status == "Pending")
                .ToList();
            foreach (var invite in openInvites)
            {
                invite.Status = "Cancelled";
                invite.RespondedAtUtc = DateTime.UtcNow;
                invite.ResponseMessage = "The inviting doctor left the clinic.";
            }

            _context.SaveChanges();

            await _doctorNotificationService.NotifyAsync(
                targetDoctorId,
                "Removed From Clinic",
                $"You were removed from {clinic.Name} by its owner.",
                "clinic_removal",
                $"/Doctor/Clinics/{targetDoctorId}");

            TempData["ClinicSuccess"] = "Doctor removed from the clinic.";
            return RedirectToAction(nameof(Clinics), new { id = doctor.DoctorID });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveAssistantFromClinic(int doctorId, int clinicId, int assistantId)
        {
            var accessResult = TryResolveDoctor(doctorId, out var doctor);
            if (accessResult != null)
                return accessResult;

            var clinic = ResolveOwnedClinic(clinicId, doctor!.DoctorID);
            if (clinic == null)
            {
                TempData["ClinicError"] = "Only the clinic owner can remove assistants.";
                return RedirectToAction(nameof(Clinics), new { id = doctor.DoctorID });
            }

            var assistant = _context.Assistants
                .FirstOrDefault(a => a.AssistantID == assistantId && a.ClinicID == clinicId);

            if (assistant == null)
            {
                TempData["ClinicError"] = "That assistant is not a member of this clinic.";
                return RedirectToAction(nameof(Clinics), new { id = doctor.DoctorID });
            }

            // Detach from the clinic. The assistant's controller guard sends them back
            // to their invitations page until they join another clinic.
            assistant.ClinicID = null;

            var clinicDoctorIds = _context.ClinicDoctors
                .Where(cd => cd.ClinicID == clinicId)
                .Select(cd => cd.DoctorID)
                .ToList();

            var doctorLinks = _context.AssistantDoctors
                .Where(ad => ad.AssistantID == assistantId && clinicDoctorIds.Contains(ad.DoctorID))
                .ToList();
            _context.AssistantDoctors.RemoveRange(doctorLinks);

            // Cancel invitations still open for this assistant in this clinic.
            var openInvites = _context.ClinicInvitations
                .Where(ci => ci.ClinicID == clinicId
                          && ci.AssistantID == assistantId
                          && (ci.Status == "Pending" || ci.Status == "PendingLeaveApproval"))
                .ToList();
            foreach (var invite in openInvites)
            {
                invite.Status = "Cancelled";
                invite.RespondedAtUtc = DateTime.UtcNow;
                invite.ResponseMessage = "The assistant was removed from the clinic.";
            }

            // A pending clinic switch out of this clinic is moot now that they've been
            // removed — leaving it Pending would block them from accepting a new invite.
            var pendingLeaves = _context.AssistantLeaveRequests
                .Include(r => r.Approvals)
                .Where(r => r.AssistantID == assistantId
                         && r.OldClinicID == clinicId
                         && r.Status == "Pending")
                .ToList();
            foreach (var leave in pendingLeaves)
            {
                leave.Status = "Cancelled";
                leave.ResolvedAtUtc = DateTime.UtcNow;
                leave.ResolutionMessage = "The assistant was removed from the clinic.";
                foreach (var approval in leave.Approvals.Where(a => a.Status == "Pending"))
                {
                    approval.Status = "Cancelled";
                    approval.RespondedAtUtc = DateTime.UtcNow;
                }
            }

            _context.SaveChanges();

            var assistantUserId = assistant.UserID;
            if (!string.IsNullOrEmpty(assistantUserId))
            {
                await _push.SendToUserAsync(assistantUserId,
                    "Removed From Clinic",
                    $"You were removed from {clinic.Name}. You can accept a new clinic invitation to continue.",
                    $"/Assistant/ClinicInvitations/{assistantId}");
            }

            TempData["ClinicSuccess"] = "Assistant removed from the clinic.";
            return RedirectToAction(nameof(Clinics), new { id = doctor.DoctorID });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveLeaveRequest(int id, int leaveRequestId)
        {
            var accessResult = TryResolveDoctor(id, out var doctor);
            if (accessResult != null)
                return accessResult;

            var approval = _context.AssistantLeaveApprovals
                .Include(ap => ap.LeaveRequest).ThenInclude(r => r.Approvals)
                .FirstOrDefault(ap => ap.AssistantLeaveRequestID == leaveRequestId
                                   && ap.DoctorID == doctor!.DoctorID);

            if (approval == null)
            {
                TempData["InviteError"] = "Leave request not found.";
                return RedirectToAction(nameof(Clinics), new { id = doctor!.DoctorID });
            }

            var request = approval.LeaveRequest;
            if (!string.Equals(request.Status, "Pending", StringComparison.OrdinalIgnoreCase))
            {
                TempData["InviteError"] = "This leave request has already been resolved.";
                return RedirectToAction(nameof(Clinics), new { id = doctor!.DoctorID });
            }

            if (!string.Equals(approval.Status, "Pending", StringComparison.OrdinalIgnoreCase))
            {
                TempData["InviteError"] = "You have already responded to this leave request.";
                return RedirectToAction(nameof(Clinics), new { id = doctor!.DoctorID });
            }

            approval.Status = "Approved";
            approval.RespondedAtUtc = DateTime.UtcNow;

            bool allApproved = request.Approvals.All(a => a.Status == "Approved");
            bool switchExecuted = false;

            if (allApproved)
            {
                var trackedAssistant = _context.Assistants.FirstOrDefault(a => a.AssistantID == request.AssistantID);
                var invitation = _context.ClinicInvitations.FirstOrDefault(ci => ci.ClinicInvitationID == request.ClinicInvitationID);

                if (trackedAssistant == null || invitation == null
                    || !string.Equals(invitation.Status, "PendingLeaveApproval", StringComparison.OrdinalIgnoreCase))
                {
                    // The invitation was cancelled/superseded out from under the request.
                    request.Status = "Cancelled";
                    request.ResolvedAtUtc = DateTime.UtcNow;
                    request.ResolutionMessage = "Invitation no longer valid when final approval was given.";
                }
                else
                {
                    ClinicSwitchHelper.ExecuteSwitch(_context, trackedAssistant, invitation, removeOldLinks: true);
                    request.Status = "Approved";
                    request.ResolvedAtUtc = DateTime.UtcNow;
                    request.ResolutionMessage = "Approved by all required doctors.";
                    switchExecuted = true;
                }
            }

            try
            {
                _context.SaveChanges();
            }
            catch (DbUpdateConcurrencyException)
            {
                // Another doctor resolved this request at the same instant.
                TempData["InviteError"] = "This leave request was just updated by another doctor. Please refresh.";
                return RedirectToAction(nameof(Clinics), new { id = doctor!.DoctorID });
            }

            await NotifyLeaveOutcomeAsync(request, switchExecuted, allApproved);

            TempData["InviteSuccess"] = switchExecuted
                ? "Approved. All doctors have approved — the assistant has now moved to the new clinic."
                : "Your approval was recorded. The switch executes once every required doctor approves.";
            return RedirectToAction(nameof(Clinics), new { id = doctor!.DoctorID });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DenyLeaveRequest(int id, int leaveRequestId)
        {
            var accessResult = TryResolveDoctor(id, out var doctor);
            if (accessResult != null)
                return accessResult;

            var approval = _context.AssistantLeaveApprovals
                .Include(ap => ap.LeaveRequest)
                .FirstOrDefault(ap => ap.AssistantLeaveRequestID == leaveRequestId
                                   && ap.DoctorID == doctor!.DoctorID);

            if (approval == null)
            {
                TempData["InviteError"] = "Leave request not found.";
                return RedirectToAction(nameof(Clinics), new { id = doctor!.DoctorID });
            }

            var request = approval.LeaveRequest;
            if (!string.Equals(request.Status, "Pending", StringComparison.OrdinalIgnoreCase))
            {
                TempData["InviteError"] = "This leave request has already been resolved.";
                return RedirectToAction(nameof(Clinics), new { id = doctor!.DoctorID });
            }

            // A single denial rejects the whole request.
            approval.Status = "Denied";
            approval.RespondedAtUtc = DateTime.UtcNow;
            request.Status = "Denied";
            request.ResolvedAtUtc = DateTime.UtcNow;
            request.ResolutionMessage = "Denied by a doctor in the current clinic.";

            // Release the invitation back to Pending so the assistant may retry later.
            var invitation = _context.ClinicInvitations.FirstOrDefault(ci => ci.ClinicInvitationID == request.ClinicInvitationID);
            if (invitation != null && string.Equals(invitation.Status, "PendingLeaveApproval", StringComparison.OrdinalIgnoreCase))
            {
                invitation.Status = "Pending";
                invitation.ResponseMessage = null;
            }

            try
            {
                _context.SaveChanges();
            }
            catch (DbUpdateConcurrencyException)
            {
                TempData["InviteError"] = "This leave request was just updated by another doctor. Please refresh.";
                return RedirectToAction(nameof(Clinics), new { id = doctor!.DoctorID });
            }

            await NotifyLeaveOutcomeAsync(request, switchExecuted: false, allApproved: false);

            TempData["InviteSuccess"] = "Leave request denied. The assistant remains in your clinic.";
            return RedirectToAction(nameof(Clinics), new { id = doctor!.DoctorID });
        }

        // Notifies the assistant of a leave-request outcome, and the new-clinic
        // doctor when the switch actually completes.
        private async Task NotifyLeaveOutcomeAsync(AssistantLeaveRequest request, bool switchExecuted, bool allApproved)
        {
            var assistantUserId = _context.Assistants
                .Where(a => a.AssistantID == request.AssistantID)
                .Select(a => a.UserID)
                .FirstOrDefault();

            if (string.Equals(request.Status, "Denied", StringComparison.OrdinalIgnoreCase))
            {
                if (!string.IsNullOrEmpty(assistantUserId))
                    await _push.SendToUserAsync(assistantUserId, "Clinic Change Denied",
                        "A doctor in your current clinic denied your request to switch clinics.",
                        $"/Assistant/ClinicInvitations/{request.AssistantID}");
                return;
            }

            if (switchExecuted)
            {
                if (!string.IsNullOrEmpty(assistantUserId))
                    await _push.SendToUserAsync(assistantUserId, "Clinic Change Approved",
                        "All required doctors approved. You have moved to your new clinic.",
                        $"/Assistant/ClinicInvitations/{request.AssistantID}");

                await _doctorNotificationService.NotifyAsync(
                    request.NewDoctorID,
                    "Assistant Joined Your Team",
                    "An assistant has accepted your clinic invitation and joined your team.",
                    "invitation_accepted",
                    "/Doctor/ClinicTeam");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CreateClinic(int doctorId, string clinicName, string clinicLocation)
        {
            var accessResult = TryResolveDoctor(doctorId, out var doctor);
            if (accessResult != null)
                return accessResult;

            var normalizedName = (clinicName ?? string.Empty).Trim();
            var normalizedLocation = (clinicLocation ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(normalizedName) || string.IsNullOrWhiteSpace(normalizedLocation))
            {
                TempData["ClinicError"] = "Clinic name and location are required.";
                return RedirectToAction(nameof(Clinics), new { id = doctor!.DoctorID });
            }

            var clinic = _context.Clinics.FirstOrDefault(c =>
                c.Name.ToLower() == normalizedName.ToLower() &&
                c.Location.ToLower() == normalizedLocation.ToLower());

            if (clinic == null)
            {
                clinic = new Clinic
                {
                    Name = normalizedName,
                    Location = normalizedLocation,
                    OwnerDoctorID = doctor!.DoctorID
                };

                _context.Clinics.Add(clinic);
                _context.SaveChanges();
            }

            var alreadyLinked = _context.ClinicDoctors.Any(cd =>
                cd.ClinicID == clinic.ClinicID && cd.DoctorID == doctor!.DoctorID);

            if (alreadyLinked)
            {
                TempData["ClinicSuccess"] = "Clinic already exists and is already linked to your account.";
                return RedirectToAction(nameof(Clinics), new { id = doctor!.DoctorID });
            }

            _context.ClinicDoctors.Add(new ClinicDoctor
            {
                ClinicID = clinic.ClinicID,
                DoctorID = doctor!.DoctorID
            });

            _context.SaveChanges();

            TempData["ClinicSuccess"] = "Clinic saved and linked to your account successfully.";
            return RedirectToAction(nameof(Clinics), new { id = doctor!.DoctorID });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UpdateClinicDetails(int doctorId, int clinicId, string clinicName, string clinicLocation)
        {
            var accessResult = TryResolveDoctor(doctorId, out var doctor);
            if (accessResult != null)
                return accessResult;

            var normalizedName = (clinicName ?? string.Empty).Trim();
            var normalizedLocation = (clinicLocation ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(normalizedName) || string.IsNullOrWhiteSpace(normalizedLocation))
            {
                TempData["ClinicError"] = "Clinic name and location are required.";
                return RedirectToAction(nameof(Clinics), new { id = doctor!.DoctorID });
            }

            var clinic = _context.Clinics.FirstOrDefault(c => c.ClinicID == clinicId);

            if (clinic == null || clinic.OwnerDoctorID != doctor!.DoctorID)
            {
                TempData["ClinicError"] = "Only the clinic owner can edit its details.";
                return RedirectToAction(nameof(Clinics), new { id = doctor!.DoctorID });
            }

            clinic.Name = normalizedName;
            clinic.Location = normalizedLocation;
            _context.SaveChanges();

            TempData["ClinicSuccess"] = "Clinic details updated successfully.";
            return RedirectToAction(nameof(Clinics), new { id = doctor.DoctorID });
        }

        public IActionResult Analytics(int id = 0)
        {
            var accessResult = TryResolveDoctor(id, out var doctor);
            if (accessResult != null)
                return accessResult;

            var approvedPatientLinks = _patientDoctorRepository
                .GetApprovedByDoctor(doctor.DoctorID)
                .ToList();
            var approvedPatients = approvedPatientLinks
                .Select(pd => pd.Patient)
                .Where(p => p != null)
                .ToList();

            var approvedPatientIds = approvedPatients.Select(p => p.PatientID).ToList();
            var latestBloodPressureByPatient = approvedPatientIds.Count == 0
                ? new Dictionary<int, string>()
                : _context.PatientBloodPressure
                    .Where(bp => approvedPatientIds.Contains(bp.PatientID))
                    .AsNoTracking()
                    .ToList()
                    .GroupBy(bp => bp.PatientID)
                    .Select(g => g.OrderByDescending(x => x.DateTime).First())
                    .ToDictionary(x => x.PatientID, x => x.BloodPressure);

            var appointments = _context.Appointments
                .Include(a => a.Bookings)
                .Where(a => a.DoctorID == doctor.DoctorID)
                .ToList();

            var bookingStatusCounts = appointments
                .Where(a => a.Booking != null)
                .Select(a => NormalizeDoctorBookingStatus(a.Booking!.Status))
                .Where(s => s != null)
                .GroupBy(s => s!)
                .ToDictionary(g => g.Key, g => g.Count(), StringComparer.OrdinalIgnoreCase);

            var totalAppointments = appointments.Count(a => a.isBooked);
            var completedAppointments = appointments.Count(a =>
                a.isBooked
                && a.Booking != null
                && string.Equals(a.Booking.Status, "Completed", StringComparison.OrdinalIgnoreCase));
            var completionRate = totalAppointments == 0
                ? 0
                : (int)Math.Round((double)completedAppointments / totalAppointments * 100);

            var weeklyCounts = Enumerable.Range(0, 7)
                .Select(i =>
                {
                    var targetDay = DateTime.Today.AddDays(-((int)DateTime.Today.DayOfWeek) + i).Date;
                    return appointments.Count(a => a.isBooked && a.Date.Date == targetDay);
                })
                .ToList();

            var monthlyStart = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1).AddMonths(-5);
            var monthlyTrendBuckets = Enumerable.Range(0, 6)
                .Select(i => monthlyStart.AddMonths(i))
                .Select(monthStart =>
                {
                    var monthEnd = monthStart.AddMonths(1);
                    var monthAppointments = appointments
                        .Where(a => a.Date >= monthStart && a.Date < monthEnd && a.isBooked)
                        .ToList();

                    var completedCount = monthAppointments.Count(a =>
                        a.Booking != null
                        && string.Equals(a.Booking.Status, "Completed", StringComparison.OrdinalIgnoreCase));

                    var scheduledCount = monthAppointments.Count(a =>
                        a.Booking != null
                        && (string.Equals(a.Booking.Status, "Confirmed", StringComparison.OrdinalIgnoreCase)
                            || string.Equals(a.Booking.Status, "Modified", StringComparison.OrdinalIgnoreCase)));

                    var patientCount = monthAppointments
                        .Where(a => a.PatientID.HasValue)
                        .Select(a => a.PatientID!.Value)
                        .Distinct()
                        .Count();

                    return new
                    {
                        Label = monthStart.ToString("MMM"),
                        CompletedAppointments = completedCount,
                        ScheduledAppointments = scheduledCount,
                        Patients = patientCount
                    };
                })
                .ToList();

            var monthlyLabels = monthlyTrendBuckets.Select(x => x.Label).ToList();
            var monthlyAppointmentCounts = monthlyTrendBuckets.Select(x => x.CompletedAppointments).ToList();
            var monthlyScheduledCounts = monthlyTrendBuckets.Select(x => x.ScheduledAppointments).ToList();
            var monthlyPatientCounts = monthlyTrendBuckets.Select(x => x.Patients).ToList();

            var lowRisk = approvedPatients.Count(p => ComputeRiskLevel(p, latestBloodPressureByPatient.GetValueOrDefault(p.PatientID)) == "low");
            var mediumRisk = approvedPatients.Count(p => ComputeRiskLevel(p, latestBloodPressureByPatient.GetValueOrDefault(p.PatientID)) == "medium");
            var highRisk = approvedPatients.Count(p => ComputeRiskLevel(p, latestBloodPressureByPatient.GetValueOrDefault(p.PatientID)) == "high");

            var trimesterCounts = new[]
            {
                approvedPatients.Count(p => p.GestationalWeeks <= 12),
                approvedPatients.Count(p => p.GestationalWeeks > 12 && p.GestationalWeeks <= 26),
                approvedPatients.Count(p => p.GestationalWeeks > 26)
            };

            var recentTests = _context.LabTests
                .Include(t => t.Patient)
                    .ThenInclude(p => p.User)
                .Where(t => t.DoctorID == doctor.DoctorID)
                .OrderByDescending(t => t.UploadDate)
                .Take(10)
                .Select(t => new DoctorRecentLabTestSummary
                {
                    Patient = t.Patient,
                    TestType = t.TestType,
                    UploadDate = t.UploadDate,
                    IsReviewed = !string.IsNullOrWhiteSpace(t.AI_AnalysisJSON)
                })
                .ToList();

            var vm = new DoctorAnalyticsViewModel
            {
                Doctor = doctor,
                DoctorName = BuildDoctorName(doctor),
                ActivePatientsCount = approvedPatients.Count,
                NewPatientsThisMonth = approvedPatientLinks.Count(pd =>
                    pd.ResponseDate.HasValue
                    && pd.ResponseDate.Value.Year == DateTime.Today.Year
                    && pd.ResponseDate.Value.Month == DateTime.Today.Month),
                CompletedAppointmentsCount = completedAppointments,
                AppointmentsTrend = 0,
                CompletionRate = completionRate,
                ConfirmedAppointmentsCount = bookingStatusCounts.GetValueOrDefault("Confirmed"),
                ModifiedAppointmentsCount = bookingStatusCounts.GetValueOrDefault("Modified"),
                CancelledAppointmentsCount = bookingStatusCounts.GetValueOrDefault("Cancelled"),
                MissedAppointmentsCount = bookingStatusCounts.GetValueOrDefault("Missed"),
                HighRiskPatientsCount = highRisk,
                LowRiskCount = lowRisk,
                MediumRiskCount = mediumRisk,
                WeeklyAppointmentCounts = weeklyCounts,
                MonthlyTrendLabels = monthlyLabels,
                MonthlyAppointmentCounts = monthlyAppointmentCounts,
                MonthlyScheduledCounts = monthlyScheduledCounts,
                MonthlyPatientCounts = monthlyPatientCounts,
                TrimesterCounts = trimesterCounts,
                RecentLabTests = recentTests
            };

            return View(vm);
        }

        public IActionResult Profile(int id = 0, string section = "personal")
        {
            var accessResult = TryResolveDoctor(id, out var doctor);
            if (accessResult != null)
                return accessResult;

            var allowedSections = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "personal", "professional", "clinics", "security"
            };
            var normalizedSection = allowedSections.Contains(section ?? string.Empty)
                ? section!.ToLowerInvariant()
                : "personal";

            var clinicsQuery = _context.ClinicDoctors
                .Include(cd => cd.Clinic)
                .Where(cd => cd.DoctorID == doctor.DoctorID)
                .Select(cd => cd.Clinic);

            var clinic = clinicsQuery.FirstOrDefault();
            var clinicsConnectedCount = clinicsQuery.Count();

            var appointmentsCount = _context.Appointments.Count(a => a.DoctorID == doctor.DoctorID && a.isBooked);
            var patientCount = _patientDoctorRepository.GetApprovedByDoctor(doctor.DoctorID).Count();

            var vm = new DoctorProfileViewModel
            {
                Doctor = doctor,
                DoctorName = BuildDoctorName(doctor),
                ActiveSection = normalizedSection,
                ClinicsConnectedCount = clinicsConnectedCount,
                ClinicName = clinic?.Name,
                ClinicAddress = clinic?.Location,
                WorkingHours = "By appointment",
                ConsultationFee = 0,
                Languages = "Arabic, English",
                YearsOfExperience = 0,
                Education = "Not specified",
                ClinicPhone = doctor.User?.PhoneNumber,
                TotalPatientsEver = patientCount,
                TotalAppointments = appointmentsCount,
                AverageRating = 0,
                SatisfactionRate = 0
            };

            return View(vm);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
        {
            return RedirectToAction(nameof(Index), new { id });
        }

        public IActionResult PatientDetails(int id, int patientId)
        {
            var accessResult = TryResolveDoctor(id, out var doctor);
            if (accessResult != null)
                return accessResult;

            var isAssigned = _patientDoctorRepository
                .GetApprovedByDoctor(doctor.DoctorID)
                .Any(pd => pd.PatientID == patientId);

            if (!isAssigned)
                return Forbid();

            var patient = _context.Patients
                .Include(p => p.User)
                .FirstOrDefault(p => p.PatientID == patientId);
            if (patient == null)
                return NotFound();

            var bpHistory = _context.PatientBloodPressure
                .Where(bp => bp.PatientID == patientId)
                .OrderByDescending(bp => bp.DateTime)
                .Take(25)
                .ToList();

            var bsHistory = _context.PatientBloodSugar
                .Where(bs => bs.PatientID == patientId)
                .OrderByDescending(bs => bs.DateTime)
                .Take(25)
                .ToList();

            var labTests = _context.LabTests
                .Include(l => l.TestReport)
                .Where(l => l.PatientID == patientId)
                .OrderByDescending(l => l.UploadDate)
                .Take(50)
                .ToList();

            var appointmentHistory = _context.Appointments
                .Include(a => a.Bookings)
                .Where(a => a.PatientID == patientId && a.DoctorID == doctor.DoctorID)
                .OrderByDescending(a => a.Date)
                .ThenByDescending(a => a.Time)
                .Take(20)
                .ToList();

            var notes = _context.Notes
                .Where(n => n.PatientID == patientId && n.DoctorID == doctor.DoctorID)
                .OrderByDescending(n => n.CreatedDate)
                .Take(20)
                .ToList();

            var prescriptions = _context.Prescriptions
                .Include(p => p.Items)
                .Where(p => p.PatientID == patientId && p.DoctorID == doctor.DoctorID)
                .OrderByDescending(p => p.PrescriptionDate)
                .Take(20)
                .ToList();

            var alerts = _context.Alerts
                .Where(a => a.PatientID == patientId)
                .OrderByDescending(a => a.DateCreated)
                .Take(50)
                .ToList();

            var ultrasounds = _context.UltrasoundImages
                .Where(u => u.PatientID == patientId)
                .OrderByDescending(u => u.UploadDate)
                .Take(20)
                .ToList();

            var pregnancyRecords = _context.PregnancyRecords
                .Where(r => r.PatientID == patientId)
                .ToList();

            var activePregnancyRecord = pregnancyRecords
                .Where(r => !r.EndDate.HasValue)
                .OrderByDescending(r => r.StartDate)
                .FirstOrDefault();

            var timelineEntries = new List<MedicalHistoryEntry>();

            foreach (var bp in bpHistory)
            {
                var parts = bp.BloodPressure?.Split('/');
                var status = "normal";
                if (parts?.Length == 2 &&
                    int.TryParse(parts[0], out var sys) &&
                    int.TryParse(parts[1], out var dia))
                {
                    if (sys >= 160 || dia >= 110) status = "critical";
                    else if (sys >= 140 || dia >= 90) status = "attention";
                }

                timelineEntries.Add(new MedicalHistoryEntry
                {
                    DateTime = bp.DateTime,
                    EventType = "bp-reading",
                    Status = status,
                    Title = "Blood Pressure Reading",
                    SubTitle = $"{bp.BloodPressure} mmHg",
                    BloodPressure = bp
                });
            }

            foreach (var bs in bsHistory)
            {
                var status = bs.BloodSugar >= 200 ? "critical"
                    : bs.BloodSugar >= 140 ? "attention"
                    : "normal";

                timelineEntries.Add(new MedicalHistoryEntry
                {
                    DateTime = bs.DateTime,
                    EventType = "blood-sugar",
                    Status = status,
                    Title = "Blood Sugar Reading",
                    SubTitle = $"{bs.BloodSugar} mg/dL",
                    BloodSugar = bs
                });
            }

            foreach (var lab in labTests)
            {
                timelineEntries.Add(new MedicalHistoryEntry
                {
                    DateTime = lab.UploadDate,
                    EventType = "lab-test",
                    Status = "normal",
                    Title = $"{lab.TestType} Test",
                    SubTitle = "Lab result uploaded",
                    LabTest = lab
                });
            }

            foreach (var us in ultrasounds)
            {
                var hasAnomaly = !string.IsNullOrWhiteSpace(us.DetectedAnomaly);
                timelineEntries.Add(new MedicalHistoryEntry
                {
                    DateTime = us.UploadDate,
                    EventType = "ultrasound",
                    Status = hasAnomaly ? "attention" : "normal",
                    Title = "Ultrasound Scan",
                    SubTitle = hasAnomaly ? us.DetectedAnomaly : "No anomalies detected",
                    Ultrasound = us
                });
            }

            foreach (var note in notes)
            {
                timelineEntries.Add(new MedicalHistoryEntry
                {
                    DateTime = note.CreatedDate,
                    EventType = "doctor-note",
                    Status = "normal",
                    Title = "Doctor Note",
                    SubTitle = note.Content,
                    DoctorNote = note
                });
            }

            foreach (var rx in prescriptions)
            {
                var itemCount = rx.Items?.Count ?? 0;
                timelineEntries.Add(new MedicalHistoryEntry
                {
                    DateTime = rx.PrescriptionDate,
                    EventType = "medication",
                    Status = "normal",
                    Title = "Prescription Issued",
                    SubTitle = itemCount > 0
                        ? $"{itemCount} medication{(itemCount != 1 ? "s" : string.Empty)} prescribed"
                        : (string.IsNullOrWhiteSpace(rx.Notes) ? "Prescription record" : rx.Notes),
                    Prescription = rx
                });
            }

            foreach (var record in pregnancyRecords)
            {
                timelineEntries.Add(new MedicalHistoryEntry
                {
                    DateTime = record.StartDate,
                    EventType = "pregnancy-started",
                    Status = "normal",
                    Title = "Pregnancy Started",
                    SubTitle = "Pregnancy tracking started"
                });

                if (record.EndDate.HasValue)
                {
                    timelineEntries.Add(new MedicalHistoryEntry
                    {
                        DateTime = record.EndDate.Value,
                        EventType = "pregnancy-ended",
                        Status = "normal",
                        Title = "Pregnancy Ended",
                        SubTitle = "Pregnancy was marked as ended"
                    });
                }
            }

            timelineEntries = timelineEntries
                .OrderByDescending(e => e.DateTime)
                .ToList();

            var nextAppointment = appointmentHistory
                .Where(a => a.Date.Date >= DateTime.Today && a.isBooked)
                .OrderBy(a => a.Date)
                .ThenBy(a => a.Time)
                .FirstOrDefault();

            var vm = new DoctorPatientDetailsViewModel
            {
                Doctor = doctor,
                DoctorName = BuildDoctorName(doctor),
                Patient = patient,
                RiskLevel = ComputeRiskLevel(patient, bpHistory.FirstOrDefault()?.BloodPressure),
                BabyGender = activePregnancyRecord?.BabyGender,
                ExpectedDeliveryDate = patient.DateOfPregnancy?.AddDays(280),
                LastBloodPressure = bpHistory.FirstOrDefault()?.BloodPressure,
                LastBPDate = bpHistory.FirstOrDefault()?.DateTime,
                LastBloodSugar = bsHistory.FirstOrDefault()?.BloodSugar ?? 0,
                LastBSDate = bsHistory.FirstOrDefault()?.DateTime,
                NextAppointment = nextAppointment,
                BloodPressureHistory = bpHistory,
                BloodSugarHistory = bsHistory,
                LabTests = labTests,
                AppointmentHistory = appointmentHistory,
                ClinicalNotes = notes,
                Prescriptions = prescriptions,
                AlertRecords = alerts,
                TimelineEntries = timelineEntries
            };

            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> LabReport(int id, int labTestId, CancellationToken cancellationToken)
        {
            var accessResult = TryResolveDoctor(id, out var doctor);
            if (accessResult != null)
                return accessResult;

            if (labTestId <= 0)
                return BadRequest(new { error = "Invalid lab test id." });

            var labTest = await _context.LabTests
                .AsNoTracking()
                .FirstOrDefaultAsync(l => l.LabTestID == labTestId, cancellationToken);

            if (labTest == null)
                return NotFound();

            var isAssigned = _patientDoctorRepository
                .GetApprovedByDoctor(doctor.DoctorID)
                .Any(pd => pd.PatientID == labTest.PatientID);

            if (!isAssigned)
                return Forbid();

            var result = await _analysisService.GetAnalysisResultAsync(labTestId, cancellationToken);
            if (result == null)
                return NotFound();

            return Ok(result);
        }

        [HttpGet]
        public IActionResult PrintPrescription(int id, int prescriptionId)
        {
            var accessResult = TryResolveDoctor(id, out var doctor);
            if (accessResult != null)
                return accessResult;

            var prescription = _context.Prescriptions
                .Include(p => p.Items)
                .Include(p => p.Patient)
                    .ThenInclude(pt => pt.User)
                .FirstOrDefault(p => p.PrescriptionID == prescriptionId && p.DoctorID == doctor!.DoctorID);

            if (prescription == null)
                return NotFound();

            var isAssigned = _patientDoctorRepository
                .GetApprovedByDoctor(doctor!.DoctorID)
                .Any(pd => pd.PatientID == prescription.PatientID);

            if (!isAssigned)
                return Forbid();

            var clinic = _context.ClinicDoctors
                .Include(cd => cd.Clinic)
                .Where(cd => cd.DoctorID == doctor.DoctorID)
                .Select(cd => cd.Clinic)
                .FirstOrDefault();

            var followUp = _context.Appointments
                .Where(a => a.DoctorID == doctor.DoctorID
                         && a.PatientID == prescription.PatientID
                         && a.Date.Date >= prescription.PrescriptionDate.Date
                         && a.isBooked)
                .OrderBy(a => a.Date)
                .ThenBy(a => a.Time)
                .Select(a => (DateTime?)a.Date.Date.Add(a.Time))
                .FirstOrDefault();

            var vm = new DoctorPrescriptionPrintViewModel
            {
                Doctor = doctor,
                DoctorName = BuildDoctorName(doctor),
                Patient = prescription.Patient,
                Prescription = prescription,
                ClinicName = clinic?.Name,
                ClinicAddress = clinic?.Location,
                ClinicPhone = doctor.User?.PhoneNumber,
                FollowUpDate = followUp
            };

            return View("~/Views/Doctor/PrintPrescription.cshtml", vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CreatePrescription(
            int id,
            int patientId,
            List<string>? medicineNames,
            List<string>? dosages,
            List<string>? frequencies,
            List<int>? durationDays,
            List<string>? instructions,
            string? notes)
        {
            var accessResult = TryResolveDoctor(id, out var doctor, true);
            if (accessResult != null)
                return accessResult;

            var isAssigned = _patientDoctorRepository
                .GetApprovedByDoctor(doctor!.DoctorID)
                .Any(pd => pd.PatientID == patientId);
            if (!isAssigned)
                return Json(new { success = false, message = "Patient is not assigned to this doctor." });

            var validMedicineNames = (medicineNames ?? new List<string>())
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Select(name => name.Trim())
                .ToList();

            if (!validMedicineNames.Any())
                return Json(new { success = false, message = "At least one medicine name is required." });

            var prescription = new Prescription
            {
                DoctorID = doctor.DoctorID,
                PatientID = patientId,
                PrescriptionDate = DateTime.Now,
                Notes = (notes ?? string.Empty).Trim(),
                Items = new List<PrescriptionItem>()
            };

            for (var i = 0; i < (medicineNames?.Count ?? 0); i++)
            {
                var currentName = medicineNames![i]?.Trim();
                if (string.IsNullOrWhiteSpace(currentName))
                    continue;

                var currentDosage = dosages != null && i < dosages.Count
                    ? (dosages[i] ?? string.Empty).Trim()
                    : string.Empty;

                var currentFrequency = frequencies != null && i < frequencies.Count
                    ? (frequencies[i] ?? string.Empty).Trim()
                    : string.Empty;

                var currentDuration = durationDays != null && i < durationDays.Count
                    ? Math.Max(0, durationDays[i])
                    : 0;

                var currentInstructions = instructions != null && i < instructions.Count
                    ? (instructions[i] ?? string.Empty).Trim()
                    : string.Empty;

                prescription.Items.Add(new PrescriptionItem
                {
                    MedicineName = currentName,
                    Dosage = currentDosage,
                    Frequency = currentFrequency,
                    DurationDays = currentDuration,
                    Instructions = currentInstructions
                });
            }

            if (!prescription.Items.Any())
                return Json(new { success = false, message = "Please provide at least one valid medicine." });

            _context.Prescriptions.Add(prescription);
            _context.SaveChanges();

            var savedItems = _context.PrescriptionItems
                .Include(i => i.Prescription)
                .Where(i => i.PrescriptionID == prescription.PrescriptionID)
                .ToList();

            foreach (var item in savedItems)
            {
                _medicationService.CreateFromPrescription(item, prescription.PrescriptionDate);
            }

            var medCount = prescription.Items.Count;
            _patientNotificationService.Notify(patientId,
                "New Prescription",
                $"Your doctor issued a new prescription with {medCount} medication{(medCount != 1 ? "s" : "")}. Check your medications.",
                PatientNotificationTypes.Prescription,
                "/Patient/Medications");

            return Json(new { success = true, message = "Prescription saved successfully.", prescriptionId = prescription.PrescriptionID });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddNote(int doctorId, int patientId, string content)
        {
            var accessResult = TryResolveDoctor(doctorId, out var doctor);
            if (accessResult != null)
                return accessResult;

            var isAssigned = _patientDoctorRepository
                .GetApprovedByDoctor(doctor!.DoctorID)
                .Any(pd => pd.PatientID == patientId);
            if (!isAssigned)
                return Forbid();

            if (string.IsNullOrWhiteSpace(content))
            {
                TempData["InviteError"] = "Note content is required.";
                return RedirectToAction(nameof(PatientDetails), new { id = doctor!.DoctorID, patientId });
            }

            var note = new Note
            {
                DoctorID = doctor!.DoctorID,
                PatientID = patientId,
                Content = content.Trim(),
                CreatedDate = DateTime.Now
            };

            _context.Notes.Add(note);

            var patientUserId = _context.Patients
                .Where(p => p.PatientID == patientId)
                .Select(p => p.UserID)
                .FirstOrDefault();

            if (!string.IsNullOrWhiteSpace(patientUserId) && !string.IsNullOrWhiteSpace(doctor.UserID))
            {
                var notePreview = note.Content.Length > 1850
                    ? note.Content[..1850] + "..."
                    : note.Content;

                _context.ChatMessages.Add(new ChatMessage
                {
                    SenderUserId = doctor.UserID,
                    ReceiverUserId = patientUserId,
                    Message = _chatMessageCrypto.Encrypt($"Doctor note: {notePreview}"),
                    SentAtUtc = DateTime.UtcNow,
                    IsRead = false
                });
            }

            _context.SaveChanges();

            _patientNotificationService.Notify(patientId,
                "New Doctor Note",
                "Your doctor added a new note to your record. Open Messages to read it.",
                PatientNotificationTypes.Note,
                "/Patient/Messages");

            return RedirectToAction(nameof(PatientDetails), new { id = doctor.DoctorID, patientId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SavePatientBabyGender(int id, int patientId, string? babyGender)
        {
            var accessResult = TryResolveDoctor(id, out var doctor);
            if (accessResult != null)
                return accessResult;

            var isAssigned = _patientDoctorRepository
                .GetApprovedByDoctor(doctor!.DoctorID)
                .Any(pd => pd.PatientID == patientId);
            if (!isAssigned)
                return Forbid();

            var activePregnancy = _context.PregnancyRecords
                .Where(r => r.PatientID == patientId && !r.EndDate.HasValue)
                .OrderByDescending(r => r.StartDate)
                .FirstOrDefault();

            if (activePregnancy == null)
            {
                TempData["PatientDetailsError"] = "Cannot update baby gender because there is no active pregnancy.";
                return RedirectToAction(nameof(PatientDetails), new { id = doctor.DoctorID, patientId });
            }

            var normalizedGender = (babyGender ?? string.Empty).Trim() switch
            {
                "Male" => "Male",
                "Female" => "Female",
                "Unknown" => "Unknown",
                _ => null
            };

            activePregnancy.BabyGender = normalizedGender;
            _context.SaveChanges();

            TempData["PatientDetailsSuccess"] = "Baby gender updated successfully.";
            return RedirectToAction(nameof(PatientDetails), new { id = doctor.DoctorID, patientId });
        }

        public IActionResult EditProfile(int id)
        {
            return RedirectToAction(nameof(Profile), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SaveProfilePersonal(int doctorId, string? firstName, string? lastName, string? phone, string? dateOfBirth)
        {
            var accessResult = TryResolveDoctor(doctorId, out var doctor, true);
            if (accessResult != null)
                return accessResult;

            if (doctor?.User == null)
                return Json(new { success = false, message = "Doctor user not found." });

            if (!string.IsNullOrWhiteSpace(dateOfBirth))
            {
                if (!DateTime.TryParse(dateOfBirth, out var parsedDob))
                    return Json(new { success = false, message = "Invalid date of birth." });

                doctor.User.DateOfBirth = parsedDob.Date;
            }

            doctor.User.FirstName = (firstName ?? string.Empty).Trim();
            doctor.User.LastName = (lastName ?? string.Empty).Trim();
            doctor.User.PhoneNumber = string.IsNullOrWhiteSpace(phone) ? null : phone.Trim();

            _context.SaveChanges();

            return Json(new { success = true, message = "Personal information updated successfully." });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SaveProfileProfessional(int doctorId, string? specialization, string? address)
        {
            var accessResult = TryResolveDoctor(doctorId, out var doctor, true);
            if (accessResult != null)
                return accessResult;

            if (doctor == null)
                return Json(new { success = false, message = "Doctor not found." });

            doctor.Specialization = string.IsNullOrWhiteSpace(specialization) ? string.Empty : specialization.Trim();
            doctor.Address = string.IsNullOrWhiteSpace(address) ? string.Empty : address.Trim();

            _context.SaveChanges();

            return Json(new { success = true, message = "Professional information updated successfully." });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SaveProfileClinic(int doctorId, string? clinicName, string? clinicAddress)
        {
            var accessResult = TryResolveDoctor(doctorId, out var doctor, true);
            if (accessResult != null)
                return accessResult;

            var clinic = _context.ClinicDoctors
                .Include(cd => cd.Clinic)
                .Where(cd => cd.DoctorID == doctor!.DoctorID)
                .Select(cd => cd.Clinic)
                .FirstOrDefault();

            if (clinic == null)
                return Json(new { success = false, message = "No clinic is connected to this doctor." });

            clinic.Name = string.IsNullOrWhiteSpace(clinicName) ? clinic.Name : clinicName.Trim();
            clinic.Location = string.IsNullOrWhiteSpace(clinicAddress) ? clinic.Location : clinicAddress.Trim();

            _context.SaveChanges();

            return Json(new { success = true, message = "Clinic information updated successfully." });
        }

        // ─── Doctor Notifications API ─────────────────────────────────────────

        [HttpGet]
        public IActionResult GetNotifications()
        {
            var accessResult = TryResolveDoctor(0, out var doctor, true);
            if (accessResult != null) return accessResult;

            var notifications = _context.DoctorNotifications
                .Where(n => n.DoctorID == doctor!.DoctorID)
                .OrderByDescending(n => n.DateCreated)
                .Take(30)
                .Select(n => new
                {
                    n.Id,
                    n.Title,
                    n.Message,
                    n.NotificationType,
                    n.DateCreated,
                    n.IsRead,
                    n.ActionUrl
                })
                .ToList();

            return Json(notifications);
        }

        [HttpGet]
        public IActionResult GetUnreadNotificationsCount()
        {
            var accessResult = TryResolveDoctor(0, out var doctor, true);
            if (accessResult != null) return accessResult;

            var count = _context.DoctorNotifications.Count(n => n.DoctorID == doctor!.DoctorID && !n.IsRead);
            return Json(new { unreadCount = count });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult MarkNotificationRead(int notificationId)
        {
            var accessResult = TryResolveDoctor(0, out var doctor, true);
            if (accessResult != null) return accessResult;

            var notification = _context.DoctorNotifications
                .FirstOrDefault(n => n.Id == notificationId && n.DoctorID == doctor!.DoctorID);

            if (notification == null)
                return Json(new { success = false });

            notification.IsRead = true;
            _context.SaveChanges();
            return Json(new { success = true });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult MarkAllNotificationsRead()
        {
            var accessResult = TryResolveDoctor(0, out var doctor, true);
            if (accessResult != null) return accessResult;

            var unread = _context.DoctorNotifications
                .Where(n => n.DoctorID == doctor!.DoctorID && !n.IsRead)
                .ToList();

            foreach (var n in unread)
                n.IsRead = true;

            _context.SaveChanges();
            return Json(new { success = true });
        }

        // ─────────────────────────────────────────────────────────────────────

        private IActionResult? TryResolveDoctor(int id, out Doctor? doctor, bool returnJsonOnFailure = false)
        {
            doctor = null;

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
            {
                if (returnJsonOnFailure)
                    return Unauthorized(new { success = false, message = "Unauthorized." });

                return Unauthorized();
            }

            doctor = _context.Doctors
                .Include(d => d.User)
                .FirstOrDefault(d => d.UserID == userId);

            if (doctor == null)
            {
                if (returnJsonOnFailure)
                    return Json(new { success = false, message = "Doctor not found." });

                return NotFound();
            }

            if (id > 0)
            {
                if (doctor.DoctorID != id)
                {
                    if (returnJsonOnFailure)
                        return StatusCode(StatusCodes.Status403Forbidden, new { success = false, message = "Access denied." });

                    return Forbid();
                }
            }

            return null;
        }

        private static string BuildDoctorName(Doctor doctor)
        {
            var first = doctor.User?.FirstName?.Trim() ?? string.Empty;
            var last = doctor.User?.LastName?.Trim() ?? string.Empty;
            var full = $"{first} {last}".Trim();
            return string.IsNullOrWhiteSpace(full) ? "Doctor" : full;
        }

        private static string BuildPatientName(Patient patient)
        {
            var first = patient.User?.FirstName?.Trim() ?? string.Empty;
            var last = patient.User?.LastName?.Trim() ?? string.Empty;
            var full = $"{first} {last}".Trim();
            return string.IsNullOrWhiteSpace(full) ? "Patient" : full;
        }

        private List<DoctorPatientSummary> BuildPatientSummaries(int doctorId, List<Patient> patients)
        {
            var patientIds = patients.Select(p => p.PatientID).ToList();

            if (patientIds.Count == 0)
                return new List<DoctorPatientSummary>();

            var latestBloodPressure = _context.PatientBloodPressure
                .Where(bp => patientIds.Contains(bp.PatientID))
                .AsNoTracking()
                .ToList()
                .GroupBy(bp => bp.PatientID)
                .Select(g => g.OrderByDescending(x => x.DateTime).First())
                .ToDictionary(x => x.PatientID, x => x.BloodPressure);

            var latestBloodSugar = _context.PatientBloodSugar
                .Where(bs => patientIds.Contains(bs.PatientID))
                .AsNoTracking()
                .ToList()
                .GroupBy(bs => bs.PatientID)
                .Select(g => g.OrderByDescending(x => x.DateTime).First())
                .ToDictionary(x => x.PatientID, x => (double?)x.BloodSugar);

            var latestWeight = _context.WeightTrackings
                .Where(w => patientIds.Contains(w.PatientID))
                .AsNoTracking()
                .ToList()
                .GroupBy(w => w.PatientID)
                .Select(g => g.OrderByDescending(x => x.RecordedDate).First())
                .ToDictionary(x => x.PatientID, x => (double?)x.WeightKg);

            var latestVisitDates = _context.Appointments
                .Where(a => a.DoctorID == doctorId
                         && a.PatientID.HasValue
                         && patientIds.Contains(a.PatientID.Value)
                         && a.Date.Date <= DateTime.Today
                         && a.isBooked)
                .AsNoTracking()
                .ToList()
                .GroupBy(a => a.PatientID!.Value)
                .Select(g => new
                {
                    PatientID = g.Key,
                    LastDate = g.Max(a => a.Date)
                })
                .ToDictionary(x => x.PatientID, x => (DateTime?)x.LastDate);

            var nextAppointments = _context.Appointments
                .Where(a => a.DoctorID == doctorId
                         && a.PatientID.HasValue
                         && patientIds.Contains(a.PatientID.Value)
                         && a.Date.Date >= DateTime.Today
                         && a.isBooked)
                .AsNoTracking()
                .ToList()
                .GroupBy(a => a.PatientID!.Value)
                .Select(g => new
                {
                    PatientID = g.Key,
                    NextDate = g.Min(a => a.Date)
                })
                .ToDictionary(x => x.PatientID, x => (DateTime?)x.NextDate);

            return patients
                .Select(p =>
                {
                    var latestBp = latestBloodPressure.GetValueOrDefault(p.PatientID);
                    var risk = ComputeRiskLevel(p, latestBp);
                    return new DoctorPatientSummary
                    {
                        PatientID = p.PatientID,
                        User = p.User,
                        GestationalAge = p.GestationalWeeks,
                        RiskLevel = risk,
                        NeedsAttention = risk == "high",
                        BloodType = null,
                        NextAppointmentDate = nextAppointments.GetValueOrDefault(p.PatientID),
                        LastBloodPressure = latestBp,
                        LastBloodSugar = latestBloodSugar.GetValueOrDefault(p.PatientID),
                        LastWeightKg = latestWeight.GetValueOrDefault(p.PatientID) ?? (p.WeightKg > 0 ? p.WeightKg : null),
                        LastVisitDate = latestVisitDates.GetValueOrDefault(p.PatientID)
                    };
                })
                .OrderByDescending(p => p.NeedsAttention)
                .ThenByDescending(p => p.GestationalAge)
                .ToList();
        }

        private static string ComputeRiskLevel(Patient patient, string? latestBloodPressure = null)
        {
            if (patient.BloodPressureIssue
                || IsHighBloodPressure(latestBloodPressure)
                || RiskStateIsHigh(patient.RiskState))
                return "high";

            if (patient.GestationalWeeks >= 30 || RiskStateIsMedium(patient.RiskState))
                return "medium";

            return "low";
        }

        private static bool RiskStateIsHigh(string? riskState) =>
            !string.IsNullOrWhiteSpace(riskState)
            && riskState.Contains("high", StringComparison.OrdinalIgnoreCase);

        private static bool RiskStateIsMedium(string? riskState) =>
            !string.IsNullOrWhiteSpace(riskState)
            && (riskState.Contains("moderate", StringComparison.OrdinalIgnoreCase)
                || riskState.Contains("medium", StringComparison.OrdinalIgnoreCase));

        private static bool IsHighBloodPressure(string? bloodPressure)
        {
            if (string.IsNullOrWhiteSpace(bloodPressure))
                return false;

            var parts = bloodPressure.Split('/', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 2)
                return false;

            if (!int.TryParse(parts[0], out var systolic))
                return false;

            if (!int.TryParse(parts[1], out var diastolic))
                return false;

            return systolic >= 140 || diastolic >= 90;
        }

        private static string? NormalizeDoctorBookingStatus(string? status)
        {
            return (status ?? string.Empty).Trim().ToLowerInvariant() switch
            {
                "confirmed" => "Confirmed",
                "modified" => "Modified",
                "cancelled" => "Cancelled",
                "canceled" => "Cancelled",
                "completed" => "Completed",
                "missed" => "Missed",
                _ => null
            };
        }
    }
}
