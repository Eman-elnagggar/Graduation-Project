using Graduation_Project.Interfaces;
using Graduation_Project.Data;
using Graduation_Project.Hubs;
using Graduation_Project.Models;
using Graduation_Project.Services;
using Graduation_Project.ViewModels;
using Graduation_Project.ViewModels.Chat;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Graduation_Project.Controllers
{
    [Authorize(Roles = "Patient")]
    public class PatientController : Controller
    {
        private readonly IPatient _patientRepository;
        private readonly IPatientBloodPressure _patientBloodPressure;
        private readonly IPatientBloodSugar _patientBloodSugar;
        private readonly ILabTest _labTest;
        private readonly IAppointment _appointment;
        private readonly IUltrasoundImage _ultrasoundImage;
        private readonly IPatientDoctor _patientDoctorRepository;
        private readonly IAlert _alertRepository;
        private readonly AlertService _alertService;
        private readonly IDoctorNotificationService _doctorNotificationService;
        private readonly MedicationReminderService _medicationReminderService;
        private readonly AppDbContext _context;
        private readonly IChatMessageCrypto _chatMessageCrypto;
        private readonly IWebHostEnvironment _env;
        private readonly IHubContext<ChatHub> _hubContext;

        public PatientController(
            IPatient patientRepository,
            IPatientBloodPressure patientBloodPressure,
            IPatientBloodSugar patientBloodSugar,
            ILabTest labTest,
            IAppointment appointment,
            IUltrasoundImage ultrasoundImage,
            IPatientDoctor patientDoctorRepository,
            IAlert alertRepository,
            AlertService alertService,
            IDoctorNotificationService doctorNotificationService,
            MedicationReminderService medicationReminderService,
            AppDbContext context,
            IChatMessageCrypto chatMessageCrypto,
            IWebHostEnvironment env,
            IHubContext<ChatHub> hubContext)
        {
            _patientRepository = patientRepository;
            _patientBloodPressure = patientBloodPressure;
            _patientBloodSugar = patientBloodSugar;
            _labTest = labTest;
            _appointment = appointment;
            _ultrasoundImage = ultrasoundImage;
            _patientDoctorRepository = patientDoctorRepository;
            _alertRepository = alertRepository;
            _alertService = alertService;
            _doctorNotificationService = doctorNotificationService;
            _medicationReminderService = medicationReminderService;
            _context = context;
            _chatMessageCrypto = chatMessageCrypto;
            _env = env;
            _hubContext = hubContext;
        }

        [HttpGet]
        public IActionResult UltrasoundHistory(int id)
        {
            var (patient, failure) = AuthorizePatientAccess(id);
            if (failure != null)
                return failure;

            var scans = _ultrasoundImage.GetUltrasoundsByPatientId(id).ToList();

            var patientName = patient?.User != null
                ? $"{patient.User.FirstName} {patient.User.LastName}".Trim()
                : "Patient";

            var vm = new ViewModels.Ultrasound.PatientUltrasoundHistoryViewModel
            {
                Patient = patient!,
                PatientName = patientName,
                DoctorScans = scans.Where(s => !s.IsPatientUploaded).ToList(),
                SelfScans = scans.Where(s => s.IsPatientUploaded).ToList()
            };

            return View(vm);
        }

        public IActionResult Index(int id)
        {
            var (patient, failure) = AuthorizePatientAccess(id);
            if (failure != null)
                return failure;

            var pregnancyRecords = _context.PregnancyRecords
                .Where(r => r.PatientID == id)
                .OrderByDescending(r => r.StartDate)
                .ToList();

            var activePregnancy = pregnancyRecords.FirstOrDefault(r => !r.EndDate.HasValue);
            var hasActivePregnancy = activePregnancy != null;

            // Calculate current pregnancy week
            int currentWeek = 0;
            if (hasActivePregnancy)
            {
                int daysSinceStart = (int)(DateTime.Today - activePregnancy!.StartDate.Date).TotalDays;
                currentWeek = Math.Clamp(daysSinceStart / 7, 0, 40);
            }
            else if (patient.GestationalWeeks > 0)
            {
                currentWeek = Math.Clamp(patient.GestationalWeeks, 0, 40);
            }

            // Calculate due date (280 days = 40 weeks from start)
            string dueDate = hasActivePregnancy
                ? activePregnancy!.StartDate.AddDays(280).ToString("MMM dd, yyyy")
                : "N/A";

            // Fetch latest health readings
            var lastBP = _patientBloodPressure.GetLastBloodPressureValue(id);
            var lastBS = _patientBloodSugar.GetLastBloodSugarValue(id);
            var lastLab = _labTest.GetLastLabTestByPatientId(id);
            var nextAppt = _appointment.GetNextAppointmentForPatient(id);
            var recentPastAppointments = _appointment.GetPastByPatientId(id).Take(4).ToList();

            // Fetch recent readings for the tracker panels
            var recentBPReadings = _patientBloodPressure.GetRecentByPatientId(id, 10).ToList();
            var recentBSReadings = _patientBloodSugar.GetRecentByPatientId(id, 10).ToList();

            // Fetch a larger window for weekly chart aggregation
            var weeklyBPReadings = _patientBloodPressure.GetRecentByPatientId(id, 40).ToList();
            var weeklyBSReadings = _patientBloodSugar.GetRecentByPatientId(id, 40).ToList();

            // Evaluate patient data and persist any new critical alerts.
            // Pass ALL recent readings so every abnormal value generates an alert,
            // not just whichever reading happens to be "last".
            _alertService.EvaluateAndSaveAlerts(id, patient, recentBPReadings, recentBSReadings, lastLab, nextAppt);
            _medicationReminderService.EvaluateReminders(DateTime.Today);

            // Load unread alerts for the dashboard (most recent 5)
            var unreadAlerts = _alertRepository
                .GetByPatientId(id)
                .Where(a => !a.IsRead)
                .ToList();

            var healthAlerts = unreadAlerts
                .Take(5)
                .ToList();

            var pendingRiskAlerts = unreadAlerts
                .Where(a => IsRiskAlertType(a.AlertType))
                .ToList();

            // Build recent activity feed
            var activities = new List<RecentActivityItem>();

            if (lastBP != null)
            {
                activities.Add(new RecentActivityItem
                {
                    Title = "Blood Pressure Recorded",
                    Description = $"{lastBP.BloodPressure} mmHg",
                    DateTime = lastBP.DateTime,
                    IconClass = "fas fa-heartbeat",
                    IconBgColor = "#e3f2fd",
                    IconColor = "#2196f3"
                });
            }

            if (lastBS != null)
            {
                activities.Add(new RecentActivityItem
                {
                    Title = "Blood Sugar Recorded",
                    Description = $"{lastBS.BloodSugar} mg/dL",
                    DateTime = lastBS.DateTime,
                    IconClass = "fas fa-tint",
                    IconBgColor = "#fce4ec",
                    IconColor = "#e91e63"
                });
            }

            if (lastLab != null)
            {
                activities.Add(new RecentActivityItem
                {
                    Title = $"{lastLab.TestType} Test Uploaded",
                    Description = "AI Analysis Complete",
                    DateTime = lastLab.UploadDate,
                    IconClass = "fas fa-flask",
                    IconBgColor = "#e8f5e9",
                    IconColor = "#4caf50"
                });
            }

            var lastUltrasound = _ultrasoundImage.GetLastUltrasoundByPatientId(id);
            if (lastUltrasound != null)
            {
                activities.Add(new RecentActivityItem
                {
                    Title = "Ultrasound Analyzed",
                    Description = string.IsNullOrWhiteSpace(lastUltrasound.DetectedAnomaly)
                        ? "No anomalies detected"
                        : lastUltrasound.DetectedAnomaly,
                    DateTime = lastUltrasound.UploadDate,
                    IconClass = "fas fa-baby",
                    IconBgColor = "#f3e5f5",
                    IconColor = "#9c27b0"
                });
            }

            if (nextAppt != null)
            {
                activities.Add(new RecentActivityItem
                {
                    Title = "Upcoming Appointment",
                    Description = $"Dr. {nextAppt.Doctor?.User?.FirstName} - {nextAppt.Date:MMM dd, yyyy}",
                    DateTime = DateTime.Now,
                    OverrideTime = nextAppt.Date.ToString("MMM dd, yyyy"),
                    IconClass = "fas fa-calendar-check",
                    IconBgColor = "#fff3e0",
                    IconColor = "#ff9800"
                });
            }

            var latestEndedPregnancy = pregnancyRecords
                .Where(r => r.EndDate.HasValue)
                .OrderByDescending(r => r.EndDate)
                .FirstOrDefault();

            if (latestEndedPregnancy?.EndDate.HasValue == true)
            {
                activities.Add(new RecentActivityItem
                {
                    Title = "Pregnancy Ended",
                    Description = $"Recorded on {latestEndedPregnancy.EndDate.Value:MMM dd, yyyy}",
                    DateTime = latestEndedPregnancy.EndDate.Value,
                    IconClass = "fas fa-flag-checkered",
                    IconBgColor = "#fff8e1",
                    IconColor = "#ffb300"
                });
            }

            // Sort by most recent first, keep top 5
            activities = activities
                .OrderByDescending(a => a.DateTime)
                .Take(5)
                .ToList();

            var viewModel = new PatientDashboardViewModel
            {
                Patient = patient,
                UserName = patient.User?.FirstName ?? "Patient",
                HasActivePregnancy = hasActivePregnancy,
                PregnancyWeek = currentWeek,
                PregnancyProgressPercent = (int)Math.Round(currentWeek / 40.0 * 100),
                Trimester = !hasActivePregnancy ? "Not Active"
                          : currentWeek <= 13 ? "1st Trimester"
                          : currentWeek <= 26 ? "2nd Trimester"
                          : "3rd Trimester",
                DueDate = dueDate,
                LastBloodPressureValue = lastBP?.BloodPressure ?? "N/A",
                LastBloodSugarValue = lastBS?.BloodSugar ?? 0,
                LastLabTest = lastLab,
                NextAppointment = nextAppt,
                RecentPastAppointments = recentPastAppointments,
                RecentBloodPressureReadings = recentBPReadings,
                RecentBloodSugarReadings = recentBSReadings,
                WeeklyBloodPressureReadings = weeklyBPReadings,
                WeeklyBloodSugarReadings = weeklyBSReadings,
                RecentActivities = activities,
                HealthAlerts = healthAlerts,
                PendingRiskAlerts = pendingRiskAlerts
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EndCurrentPregnancy(int id, string? returnUrl = null)
        {
            var (patient, failure) = AuthorizePatientAccess(id);
            if (failure != null)
                return failure;

            var activePregnancy = _context.PregnancyRecords
                .Where(r => r.PatientID == id && !r.EndDate.HasValue)
                .OrderByDescending(r => r.StartDate)
                .FirstOrDefault();

            if (activePregnancy == null)
            {
                TempData["PregnancyStatusMessage"] = "No active pregnancy found to end.";
                return RedirectToLocalOrDashboard(id, returnUrl);
            }

            activePregnancy.EndDate = DateTime.Now;

            // Keep legacy fields in sync until all old columns are removed.
            patient.LastPregnancyStartedAt = activePregnancy.StartDate;
            patient.PregnancyEndedAt = activePregnancy.EndDate;
            patient.DateOfPregnancy = null;
            patient.GestationalWeeks = 0;
            patient.PreviousPregnancies += 1;
            patient.IsFirstPregnancy = false;
            var pregnancyRecordsCount = _context.PregnancyRecords.Count(r => r.PatientID == id);
            patient.PregnancyCount = Math.Max(0, patient.PreviousPregnancies) + pregnancyRecordsCount;

            _patientRepository.Update(patient);
            _patientRepository.Save();

            TempData["PregnancyStatusMessage"] = "Current pregnancy was ended and saved to your history.";
            return RedirectToLocalOrDashboard(id, returnUrl);
        }

        public IActionResult Messages(int id, string? user = null)
        {
            var (patient, failure) = AuthorizePatientAccess(id);
            if (failure != null)
                return failure;

            var approvedLinks = _patientDoctorRepository
                .GetByPatientId(id)
                .Where(pd => string.Equals(pd.Status, "Approved", StringComparison.OrdinalIgnoreCase)
                          && pd.Doctor != null
                          && !string.IsNullOrWhiteSpace(pd.Doctor.UserID))
                .GroupBy(pd => pd.DoctorID)
                .Select(g => g.First())
                .ToList();

            if (string.IsNullOrWhiteSpace(patient.UserID))
                return NotFound();

            var patientUserId = patient.UserID;
            var approvedDoctorIds = approvedLinks
                .Select(pd => pd.DoctorID)
                .Distinct()
                .ToList();

            var doctorUserIds = approvedLinks
                .Select(pd => pd.Doctor!.UserID)
                .Where(userId => !string.IsNullOrWhiteSpace(userId))
                .Distinct()
                .ToList();

            var linkedAssistants = _context.AssistantDoctors
                .Where(ad => approvedDoctorIds.Contains(ad.DoctorID))
                .Include(ad => ad.Assistant)
                    .ThenInclude(a => a.User)
                .Where(ad => ad.Assistant != null && !string.IsNullOrWhiteSpace(ad.Assistant!.UserID))
                .Select(ad => ad.Assistant!)
                .GroupBy(a => a.AssistantID)
                .Select(g => g.First())
                .ToList();

            var assistantUserIds = linkedAssistants
                .Select(a => a.UserID)
                .Where(userId => !string.IsNullOrWhiteSpace(userId))
                .Distinct()
                .ToList();

            var receiverUserIds = doctorUserIds
                .Concat(assistantUserIds)
                .Distinct()
                .ToList();

            var chatMessages = _context.ChatMessages
                .Where(m => (m.SenderUserId == patientUserId && receiverUserIds.Contains(m.ReceiverUserId))
                         || (m.ReceiverUserId == patientUserId && receiverUserIds.Contains(m.SenderUserId)))
                .OrderByDescending(m => m.SentAtUtc)
                .ToList();

            var doctorConversations = approvedLinks
                .Select(pd => new
                {
                    ParticipantId = pd.DoctorID,
                    ParticipantType = "Doctor",
                    ReceiverUserId = pd.Doctor?.UserID ?? string.Empty,
                    ParticipantName = pd.Doctor?.User != null
                        ? $"Dr. {pd.Doctor.User.FirstName} {pd.Doctor.User.LastName}".Trim()
                        : "Doctor"
                });

            var assistantConversations = linkedAssistants
                .Select(a => new
                {
                    ParticipantId = a.AssistantID,
                    ParticipantType = "Assistant",
                    ReceiverUserId = a.UserID ?? string.Empty,
                    ParticipantName = a.User != null
                        ? $"{a.User.FirstName} {a.User.LastName}".Trim()
                        : "Assistant"
                });

            var conversations = doctorConversations
                .Concat(assistantConversations)
                .Where(c => !string.IsNullOrWhiteSpace(c.ReceiverUserId))
                .GroupBy(c => c.ReceiverUserId)
                .Select(g => g.First())
                .Select(c => new PatientConversationSummary
                {
                    ParticipantId = c.ParticipantId,
                    ParticipantType = c.ParticipantType,
                    ReceiverUserId = c.ReceiverUserId,
                    ParticipantName = c.ParticipantName,
                    UnreadCount = chatMessages.Count(m => m.SenderUserId == c.ReceiverUserId && m.ReceiverUserId == patientUserId && !m.IsRead),
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

            // ── Patient-to-patient (community) conversations ──────────────
            // Any other patient this user has exchanged messages with, plus an
            // optionally requested peer (?user=) opened from the community.
            var peerUserIds = _context.ChatMessages
                .Where(m => m.SenderUserId == patientUserId || m.ReceiverUserId == patientUserId)
                .Select(m => m.SenderUserId == patientUserId ? m.ReceiverUserId : m.SenderUserId)
                .Distinct()
                .ToList();

            var peerPatients = _context.Patients
                .Include(p => p.User)
                .Where(p => p.UserID != null && p.UserID != patientUserId && peerUserIds.Contains(p.UserID))
                .ToList();

            if (!string.IsNullOrWhiteSpace(user)
                && user != patientUserId
                && peerPatients.All(p => p.UserID != user))
            {
                var requestedPeer = _context.Patients
                    .Include(p => p.User)
                    .FirstOrDefault(p => p.UserID == user);
                if (requestedPeer != null)
                    peerPatients.Add(requestedPeer);
            }

            if (peerPatients.Count > 0)
            {
                var peerUserIdList = peerPatients
                    .Select(p => p.UserID!)
                    .Where(uid => !string.IsNullOrWhiteSpace(uid))
                    .Distinct()
                    .ToList();

                var peerMessages = _context.ChatMessages
                    .Where(m => (m.SenderUserId == patientUserId && peerUserIdList.Contains(m.ReceiverUserId))
                             || (m.ReceiverUserId == patientUserId && peerUserIdList.Contains(m.SenderUserId)))
                    .OrderByDescending(m => m.SentAtUtc)
                    .ToList();

                var peerConversations = peerPatients
                    .Where(p => !string.IsNullOrWhiteSpace(p.UserID))
                    .GroupBy(p => p.UserID)
                    .Select(g => g.First())
                    .Select(p => new PatientConversationSummary
                    {
                        ParticipantId = p.PatientID,
                        ParticipantType = "Patient",
                        ReceiverUserId = p.UserID!,
                        ParticipantName = p.User != null
                            ? $"{p.User.FirstName} {p.User.LastName}".Trim()
                            : "Community Member",
                        UnreadCount = peerMessages.Count(m => m.SenderUserId == p.UserID && m.ReceiverUserId == patientUserId && !m.IsRead),
                        LastMessageTime = peerMessages
                            .Where(m => m.SenderUserId == p.UserID || m.ReceiverUserId == p.UserID)
                            .Select(m => (DateTime?)m.SentAtUtc)
                            .FirstOrDefault(),
                        LastMessagePreview = peerMessages
                            .Where(m => m.SenderUserId == p.UserID || m.ReceiverUserId == p.UserID)
                            .Select(m => _chatMessageCrypto.Decrypt(m.Message))
                            .FirstOrDefault() ?? "Start a conversation"
                    })
                    .ToList();

                conversations.AddRange(peerConversations);
            }

            var vm = new PatientMessagesViewModel
            {
                Patient = patient,
                UserName = patient.User?.FirstName ?? "Patient",
                Conversations = conversations
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadChatFile(int id, IFormFile file)
        {
            var (patient, failure) = AuthorizePatientAccess(id);
            if (failure != null)
                return failure;

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

            var userDir = Path.Combine(_env.WebRootPath, "uploads", "chat", patient.UserID!);
            Directory.CreateDirectory(userDir);

            var ext = Path.GetExtension(file.FileName);
            var fileName = $"{Guid.NewGuid():N}{ext}";
            var filePath = Path.Combine(userDir, fileName);

            using (var stream = System.IO.File.Create(filePath))
                await file.CopyToAsync(stream);

            var url = $"/uploads/chat/{patient.UserID}/{fileName}";
            var type = file.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase) ? "image" : "file";

            return Json(new { url, type, name = file.FileName });
        }

        [HttpGet]
        public IActionResult ConversationMessages(int id, string userId)
        {
            var (patient, failure) = AuthorizePatientAccess(id);
            if (failure != null)
                return failure;

            var approvedDoctorIds = _patientDoctorRepository
                .GetByPatientId(id)
                .Where(pd => string.Equals(pd.Status, "Approved", StringComparison.OrdinalIgnoreCase))
                .Select(pd => pd.DoctorID)
                .Distinct()
                .ToList();

            var linkedDoctorUserIds = _context.Doctors
                .AsNoTracking()
                .Where(d => approvedDoctorIds.Contains(d.DoctorID)
                         && !string.IsNullOrWhiteSpace(d.UserID))
                .Select(d => d.UserID!)
                .ToList();

            var linkedAssistantUserIds = _context.AssistantDoctors
                .Where(ad => approvedDoctorIds.Contains(ad.DoctorID))
                .Include(ad => ad.Assistant)
                .Where(ad => ad.Assistant != null && !string.IsNullOrWhiteSpace(ad.Assistant!.UserID))
                .Select(ad => ad.Assistant!.UserID!)
                .Distinct()
                .ToList();

            var linkedUserIds = linkedDoctorUserIds
                .Concat(linkedAssistantUserIds)
                .Distinct()
                .ToList();

            // Allow conversations with linked care team OR any other patient (community DMs).
            var isPeerPatient = !string.IsNullOrWhiteSpace(userId)
                && userId != patient.UserID
                && _context.Patients.Any(p => p.UserID == userId);

            if (string.IsNullOrWhiteSpace(userId) || (!linkedUserIds.Contains(userId) && !isPeerPatient))
                return Forbid();

            if (string.IsNullOrWhiteSpace(patient.UserID))
                return NotFound();

            var patientUserId = patient.UserID;
            var receiverUserId = userId;

            var messages = _context.ChatMessages
                .Where(m => (m.SenderUserId == patientUserId && m.ReceiverUserId == receiverUserId)
                         || (m.SenderUserId == receiverUserId && m.ReceiverUserId == patientUserId))
                .OrderBy(m => m.SentAtUtc)
                .ToList()
                .Select(m => new
                {
                    id = m.ChatMessageId,
                    senderId = m.SenderUserId,
                    receiverId = m.ReceiverUserId,
                    content = _chatMessageCrypto.Decrypt(m.Message),
                    timestamp = m.SentAtUtc,
                    attachmentUrl = m.AttachmentUrl,
                    attachmentType = m.AttachmentType,
                    attachmentName = m.AttachmentName
                })
                .ToList();

            var unreadIncoming = _context.ChatMessages
                .Where(m => m.SenderUserId == receiverUserId
                         && m.ReceiverUserId == patientUserId
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

        // ---------------------------------------------------------------
        // POST: /Patient/SaveBloodPressure
        // ---------------------------------------------------------------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SaveBloodPressure(int patientId, string systolic, string diastolic, string? pulse, string? measurementTime)
        {
            var (patient, failure) = AuthorizePatientAccess(patientId, true);
            if (failure != null)
                return failure;

            if (string.IsNullOrWhiteSpace(systolic) || string.IsNullOrWhiteSpace(diastolic))
                return BadRequest(new { success = false, message = "Systolic and diastolic values are required." });

            var reading = new PatientBloodPressure
            {
                PatientID = patientId,
                BloodPressure = $"{systolic}/{diastolic}",
                DateTime = DateTime.Now,
                MeasurementTime = measurementTime
            };

            _patientBloodPressure.Add(reading);
            _patientBloodPressure.Save();

            // Evaluate and persist alerts for the new reading immediately
            if (patient != null)
            {
                var lastBS = _patientBloodSugar.GetLastBloodSugarValue(patientId);
                var lastLab = _labTest.GetLastLabTestByPatientId(patientId);
                var nextAppt = _appointment.GetNextAppointmentForPatient(patientId);
                int newAlerts = _alertService.EvaluateAndSaveAlerts(patientId, patient, reading, lastBS, lastLab, nextAppt);
                if (newAlerts > 0)
                {
                    var pName = $"{patient.User?.FirstName} {patient.User?.LastName}".Trim();
                    if (string.IsNullOrWhiteSpace(pName)) pName = "A patient";
                    var bpParts = reading.BloodPressure.Split('/');
                    int sys = int.TryParse(bpParts[0].Trim(), out var sv) ? sv : 0;
                    int dia = bpParts.Length > 1 && int.TryParse(bpParts[1].Trim(), out var dv) ? dv : 0;
                    string bpTitle, bpMsg;
                    if (sys >= 160 || dia >= 110)
                    {
                        bpTitle = $"Critical BP: {pName}";
                        bpMsg = $"{pName}'s blood pressure ({reading.BloodPressure} mmHg) is critically high. Immediate attention required.";
                    }
                    else if (sys >= 140 || dia >= 90)
                    {
                        bpTitle = $"High BP Detected: {pName}";
                        bpMsg = $"{pName}'s blood pressure ({reading.BloodPressure} mmHg) exceeds safe limits.";
                    }
                    else if (sys > 0 && (sys < 90 || dia < 60))
                    {
                        bpTitle = $"Low BP Detected: {pName}";
                        bpMsg = $"{pName}'s blood pressure ({reading.BloodPressure} mmHg) is below normal range.";
                    }
                    else
                    {
                        bpTitle = $"Health Alert: {pName}";
                        bpMsg = $"{pName} has a new blood pressure alert that requires your attention.";
                    }
                    NotifyAssignedDoctor(patientId, patient, bpTitle, bpMsg);
                }
            }

            return Json(new
            {
                success = true,
                id = reading.ID,
                bloodPressure = reading.BloodPressure,
                dateTime = reading.DateTime.ToString("MMM dd, yyyy hh:mm tt"),
                day = reading.DateTime.Day.ToString(),
                month = reading.DateTime.ToString("MMM"),
                time = reading.DateTime.ToString("h:mm tt"),
                measurementTime = reading.MeasurementTime
            });
        }

        // ---------------------------------------------------------------
        // POST: /Patient/SaveBloodSugar
        // ---------------------------------------------------------------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SaveBloodSugar(int patientId, double bloodSugar, string? measurementTime)
        {
            var (patient, failure) = AuthorizePatientAccess(patientId, true);
            if (failure != null)
                return failure;

            if (bloodSugar <= 0)
                return BadRequest(new { success = false, message = "Blood sugar value is required." });

            var reading = new PatientBloodSugar
            {
                PatientID = patientId,
                BloodSugar = bloodSugar,
                DateTime = DateTime.Now,
                MeasurementTime = measurementTime
            };

            _patientBloodSugar.Add(reading);
            _patientBloodSugar.Save();

            // Evaluate and persist alerts for the new reading immediately
            if (patient != null)
            {
                var lastBP = _patientBloodPressure.GetLastBloodPressureValue(patientId);
                var lastLab = _labTest.GetLastLabTestByPatientId(patientId);
                var nextAppt = _appointment.GetNextAppointmentForPatient(patientId);
                int newAlerts = _alertService.EvaluateAndSaveAlerts(patientId, patient, lastBP, reading, lastLab, nextAppt);
                if (newAlerts > 0)
                {
                    var pName = $"{patient.User?.FirstName} {patient.User?.LastName}".Trim();
                    if (string.IsNullOrWhiteSpace(pName)) pName = "A patient";
                    string bsTitle, bsMsg;
                    if (reading.BloodSugar >= 200)
                    {
                        bsTitle = $"Critical Blood Sugar: {pName}";
                        bsMsg = $"{pName}'s blood sugar ({reading.BloodSugar} mg/dL) is critically high. Immediate attention required.";
                    }
                    else if (reading.BloodSugar > 125)
                    {
                        bsTitle = $"High Blood Sugar: {pName}";
                        bsMsg = $"{pName}'s blood sugar ({reading.BloodSugar} mg/dL) is above normal range.";
                    }
                    else if (reading.BloodSugar < 70)
                    {
                        bsTitle = $"Low Blood Sugar: {pName}";
                        bsMsg = $"{pName}'s blood sugar ({reading.BloodSugar} mg/dL) is dangerously low. Immediate attention required.";
                    }
                    else
                    {
                        bsTitle = $"Health Alert: {pName}";
                        bsMsg = $"{pName} has a new blood sugar alert that requires your attention.";
                    }
                    NotifyAssignedDoctor(patientId, patient, bsTitle, bsMsg);
                }
            }

            return Json(new
            {
                success = true,
                id = reading.ID,
                bloodSugar = reading.BloodSugar,
                dateTime = reading.DateTime.ToString("MMM dd, yyyy hh:mm tt"),
                day = reading.DateTime.Day.ToString(),
                month = reading.DateTime.ToString("MMM"),
                time = reading.DateTime.ToString("h:mm tt"),
                measurementTime = reading.MeasurementTime
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AcknowledgeRiskAlerts(int patientId)
        {
            var (_, failure) = AuthorizePatientAccess(patientId, true);
            if (failure != null)
                return failure;

            var pendingRiskAlerts = _alertRepository
                .GetByPatientId(patientId)
                .Where(a => !a.IsRead && IsRiskAlertType(a.AlertType))
                .ToList();

            foreach (var alert in pendingRiskAlerts)
            {
                alert.IsRead = true;
                _alertRepository.Update(alert);
            }

            _alertRepository.Save();

            return Json(new { success = true, count = pendingRiskAlerts.Count });
        }

        [HttpGet]
        public IActionResult GetPendingRiskAlerts(int patientId)
        {
            var (_, failure) = AuthorizePatientAccess(patientId, true);
            if (failure != null)
                return failure;

            var pendingRiskAlerts = _alertRepository
                .GetByPatientId(patientId)
                .Where(a => !a.IsRead && IsRiskAlertType(a.AlertType))
                .OrderByDescending(a => a.DateCreated)
                .Select(a => new
                {
                    alertId = a.AlertID,
                    title = a.Title,
                    message = a.Message,
                    dateCreated = a.DateCreated.ToString("g")
                })
                .ToList();

            return Json(new { success = true, alerts = pendingRiskAlerts });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendReportToDoctor(int id, [FromBody] SendReportToDoctorRequest req)
        {
            var (patient, failure) = AuthorizePatientAccess(id);
            if (failure != null)
                return failure;

            if (string.IsNullOrWhiteSpace(req?.DoctorUserId) || string.IsNullOrWhiteSpace(req?.AttachmentUrl))
                return BadRequest(new { error = "Invalid request." });

            var approvedDoctorUserIds = _patientDoctorRepository
                .GetByPatientId(id)
                .Where(pd => string.Equals(pd.Status, "Approved", StringComparison.OrdinalIgnoreCase)
                          && pd.Doctor != null
                          && !string.IsNullOrWhiteSpace(pd.Doctor.UserID))
                .Select(pd => pd.Doctor!.UserID!)
                .Distinct()
                .ToList();

            if (!approvedDoctorUserIds.Contains(req.DoctorUserId))
                return Forbid();

            if (string.IsNullOrWhiteSpace(patient!.UserID))
                return StatusCode(500);

            var caption = string.IsNullOrWhiteSpace(req.Caption) ? "Lab Analysis Report" : req.Caption.Trim();
            var fileName = string.IsNullOrWhiteSpace(req.FileName) ? "Lab-Analysis-Report.pdf" : req.FileName;

            var chatMessage = new ChatMessage
            {
                SenderUserId = patient.UserID,
                ReceiverUserId = req.DoctorUserId,
                Message = _chatMessageCrypto.Encrypt(caption),
                SentAtUtc = DateTime.Now,
                IsRead = false,
                AttachmentUrl = req.AttachmentUrl,
                AttachmentType = "file",
                AttachmentName = fileName
            };

            _context.ChatMessages.Add(chatMessage);
            await _context.SaveChangesAsync();

            await _hubContext.Clients.User(req.DoctorUserId).SendAsync(
                "ReceiveMessage",
                patient.UserID,
                caption,
                chatMessage.SentAtUtc,
                req.AttachmentUrl,
                "file",
                fileName);

            return Json(new { success = true });
        }

        private static bool IsRiskAlertType(string? alertType)
        {
            if (string.IsNullOrWhiteSpace(alertType))
                return false;

            return alertType.Equals("danger", StringComparison.OrdinalIgnoreCase)
                || alertType.Equals("critical", StringComparison.OrdinalIgnoreCase)
                || alertType.Equals("warning", StringComparison.OrdinalIgnoreCase);
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

        private IActionResult RedirectToLocalOrDashboard(int patientId, string? returnUrl)
        {
            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return RedirectToAction(nameof(Index), new { id = patientId });
        }

        private void NotifyAssignedDoctor(int patientId, Patient patient, string? title = null, string? message = null)
        {
            var patientName = $"{patient.User?.FirstName} {patient.User?.LastName}".Trim();
            if (string.IsNullOrWhiteSpace(patientName)) patientName = "A patient";

            var notifTitle = title ?? $"Health Alert: {patientName}";
            var notifMessage = message ?? $"{patientName} has a new health risk alert that requires your attention.";

            var assignedDoctors = _patientDoctorRepository
                .GetByPatientId(patientId)
                .Where(pd => string.Equals(pd.Status, "Approved", StringComparison.OrdinalIgnoreCase))
                .ToList();

            foreach (var pd in assignedDoctors)
            {
                _ = _doctorNotificationService.NotifyAsync(
                    pd.DoctorID,
                    notifTitle,
                    notifMessage,
                    "patient_risk",
                    $"/Doctor/PatientDetails/{pd.DoctorID}?patientId={patientId}");
            }
        }
    }
}
