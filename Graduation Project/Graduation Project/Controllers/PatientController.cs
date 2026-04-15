using Graduation_Project.Interfaces;
using Graduation_Project.Data;
using Graduation_Project.Models;
using Graduation_Project.Services;
using Graduation_Project.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
        private readonly AppDbContext _context;
        private readonly IChatMessageCrypto _chatMessageCrypto;

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
            AppDbContext context,
            IChatMessageCrypto chatMessageCrypto)
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
            _context = context;
            _chatMessageCrypto = chatMessageCrypto;
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

            // Load unread alerts for the dashboard (most recent 5)
            var healthAlerts = _alertRepository
                .GetByPatientId(id)
                .Where(a => !a.IsRead)
                .Take(5)
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
                RecentBloodPressureReadings = recentBPReadings,
                RecentBloodSugarReadings = recentBSReadings,
                WeeklyBloodPressureReadings = weeklyBPReadings,
                WeeklyBloodSugarReadings = weeklyBSReadings,
                RecentActivities = activities,
                HealthAlerts = healthAlerts
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

        public IActionResult Messages(int id)
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

            var vm = new PatientMessagesViewModel
            {
                Patient = patient,
                UserName = patient.User?.FirstName ?? "Patient",
                Conversations = conversations
            };

            return View(vm);
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

            if (string.IsNullOrWhiteSpace(userId) || !linkedUserIds.Contains(userId))
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
                    timestamp = m.SentAtUtc
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
                _alertService.EvaluateAndSaveAlerts(patientId, patient, reading, lastBS, lastLab, nextAppt);
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
                _alertService.EvaluateAndSaveAlerts(patientId, patient, lastBP, reading, lastLab, nextAppt);
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
    }
}
