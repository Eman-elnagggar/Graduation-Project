using Graduation_Project.Data;
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
        private readonly IChatMessageCrypto _chatMessageCrypto;

        public DoctorController(
            IAppointment appointmentRepository,
            IPatientDoctor patientDoctorRepository,
            AppDbContext context,
            IChatMessageCrypto chatMessageCrypto)
        {
            _appointmentRepository = appointmentRepository;
            _patientDoctorRepository = patientDoctorRepository;
            _context = context;
            _chatMessageCrypto = chatMessageCrypto;
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
                WeeklyAppointmentCounts = weeklyCounts,
                NextAppointment = nextAppointment,
                TodayAppointments = todayAppointments,
                RecentAlerts = recentAlerts,
                RecentMessages = recentMessages,
                PriorityPatients = patientSummaries
                    .Where(p => p.NeedsAttention)
                    .Take(6)
                    .ToList()
            };

            return View(vm);
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
                    timestamp = m.SentAtUtc
                })
                .ToList();

            var unreadIncoming = _context.ChatMessages
                .Where(m => m.SenderUserId == receiverUserId
                         && m.ReceiverUserId == doctorUserId
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

        public IActionResult ClinicTeam(int id = 0)
        {
            var accessResult = TryResolveDoctor(id, out var doctor);
            if (accessResult != null)
                return accessResult;

            var assistantIds = _context.AssistantDoctors
                .Where(ad => ad.DoctorID == doctor.DoctorID)
                .Select(ad => ad.AssistantID)
                .ToList();

            var assistants = _context.Assistants
                .Include(a => a.User)
                .Where(a => assistantIds.Contains(a.AssistantID))
                .OrderBy(a => a.User.FirstName)
                .ToList();

            var vm = new DoctorClinicTeamViewModel
            {
                Doctor = doctor,
                DoctorName = BuildDoctorName(doctor),
                Assistants = assistants,
                PendingInvitations = new List<PendingInvitationViewModel>()
            };

            return View(vm);
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
                .Where(l => l.PatientID == patientId && l.DoctorID == doctor.DoctorID)
                .OrderByDescending(l => l.UploadDate)
                .Take(20)
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

            foreach (var appt in appointmentHistory)
            {
                timelineEntries.Add(new MedicalHistoryEntry
                {
                    DateTime = appt.Date.Date.Add(appt.Time),
                    EventType = "appointment",
                    Status = "normal",
                    Title = string.IsNullOrWhiteSpace(appt.Booking?.Reason) ? "Consultation" : appt.Booking.Reason,
                    SubTitle = $"Appointment {((appt.Date.Date.Add(appt.Time) < DateTime.Now) ? "completed" : "upcoming")}",
                    Appointment = appt
                });
            }

            foreach (var alert in alerts)
            {
                var status = (alert.AlertType ?? "").ToLowerInvariant() switch
                {
                    "danger" => "critical",
                    "critical" => "critical",
                    "warning" => "attention",
                    _ => "normal"
                };

                timelineEntries.Add(new MedicalHistoryEntry
                {
                    DateTime = alert.DateCreated,
                    EventType = "alert",
                    Status = status,
                    Title = alert.Title,
                    SubTitle = alert.Message,
                    Alert = alert
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
                    SentAtUtc = DateTime.Now,
                    IsRead = false
                });
            }

            _context.SaveChanges();

            return RedirectToAction(nameof(PatientDetails), new { id = doctor.DoctorID, patientId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult InviteAssistant(int doctorId, string assistantEmail)
        {
            var accessResult = TryResolveDoctor(doctorId, out var doctor);
            if (accessResult != null)
                return accessResult;

            if (string.IsNullOrWhiteSpace(assistantEmail))
            {
                TempData["InviteError"] = "Assistant email is required.";
                return RedirectToAction(nameof(ClinicTeam), new { id = doctor!.DoctorID });
            }

            var assistant = _context.Assistants
                .Include(a => a.User)
                .FirstOrDefault(a => a.User.Email == assistantEmail.Trim());

            if (assistant == null)
            {
                TempData["InviteError"] = "No assistant account found with this email.";
                return RedirectToAction(nameof(ClinicTeam), new { id = doctor!.DoctorID });
            }

            var exists = _context.AssistantDoctors.Any(ad =>
                ad.DoctorID == doctor!.DoctorID && ad.AssistantID == assistant.AssistantID);
            if (exists)
            {
                TempData["InviteError"] = "Assistant is already linked to your clinic team.";
                return RedirectToAction(nameof(ClinicTeam), new { id = doctor.DoctorID });
            }

            _context.AssistantDoctors.Add(new AssistantDoctor
            {
                DoctorID = doctor.DoctorID,
                AssistantID = assistant.AssistantID
            });
            _context.SaveChanges();

            TempData["InviteSuccess"] = "Assistant added to clinic team successfully.";
            return RedirectToAction(nameof(ClinicTeam), new { id = doctor.DoctorID });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RemoveAssistant(int doctorId, int assistantId)
        {
            var accessResult = TryResolveDoctor(doctorId, out var doctor);
            if (accessResult != null)
                return accessResult;

            var link = _context.AssistantDoctors
                .FirstOrDefault(ad => ad.DoctorID == doctor!.DoctorID && ad.AssistantID == assistantId);

            if (link != null)
            {
                _context.AssistantDoctors.Remove(link);
                _context.SaveChanges();
                TempData["InviteSuccess"] = "Assistant removed from your clinic team.";
            }

            return RedirectToAction(nameof(ClinicTeam), new { id = doctor!.DoctorID });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CancelInvitation(int invitationId, int id)
        {
            TempData["InviteError"] = "Invitation cancellation is not available yet.";
            return RedirectToAction(nameof(ClinicTeam), new { id });
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
            if (patient.BloodPressureIssue || IsHighBloodPressure(latestBloodPressure))
                return "high";

            if (patient.GestationalWeeks >= 30)
                return "medium";

            return "low";
        }

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
