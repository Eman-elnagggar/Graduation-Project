using Graduation_Project.Data;
using Graduation_Project.Interfaces;
using Graduation_Project.Models;
using Microsoft.EntityFrameworkCore;

namespace Graduation_Project.Services
{
    public class PatientNotificationService : IPatientNotificationService
    {
        private readonly AppDbContext _context;
        private readonly IPushNotificationService _push;

        public PatientNotificationService(AppDbContext context, IPushNotificationService push)
        {
            _context = context;
            _push = push;
        }

        public int Notify(int patientId, string title, string message, string type,
                          string? actionUrl = null, bool dedupePerDay = false, string severity = "info",
                          bool sendPush = true)
        {
            if (dedupePerDay)
            {
                var today = DateTime.Today;
                bool existsToday = _context.PatientNotifications.Any(n =>
                    n.PatientID == patientId
                    && n.DateCreated.Date == today
                    && n.Title == title
                    && n.Message == message);

                if (existsToday)
                    return 0;
            }

            _context.PatientNotifications.Add(new PatientNotification
            {
                PatientID = patientId,
                Title = title,
                Message = message,
                NotificationType = type,
                Severity = severity,
                DateCreated = DateTime.Now,
                IsRead = false,
                ActionUrl = actionUrl
            });
            _context.SaveChanges();

            // Operational notifications are clinic-facing (surfaced to assistants),
            // so they are not web-pushed to the patient's browser. Callers that send
            // their own push (e.g. ChatHub) pass sendPush:false to avoid duplicates.
            if (sendPush && type != PatientNotificationTypes.Operational)
            {
                var patientUserId = _context.Patients
                    .Where(p => p.PatientID == patientId)
                    .Select(p => p.UserID)
                    .FirstOrDefault();

                if (!string.IsNullOrEmpty(patientUserId))
                    _ = _push.SendToUserAsync(patientUserId, title, message, actionUrl ?? "/Patient/Index");
            }

            return 1;
        }

        public List<PatientNotification> GetForPatient(int patientId, int take = 50)
        {
            return _context.PatientNotifications
                .Where(n => n.PatientID == patientId
                            && n.NotificationType != PatientNotificationTypes.Operational)
                .OrderByDescending(n => n.DateCreated)
                .Take(Math.Clamp(take, 1, 100))
                .ToList();
        }

        public List<PatientNotification> GetForPatients(IEnumerable<int> patientIds, string? type = null)
        {
            var ids = patientIds.ToList();
            var query = _context.PatientNotifications
                .Include(n => n.Patient)
                    .ThenInclude(p => p.User)
                .Where(n => ids.Contains(n.PatientID));

            if (!string.IsNullOrWhiteSpace(type))
                query = query.Where(n => n.NotificationType == type);

            return query
                .OrderByDescending(n => n.DateCreated)
                .ToList();
        }

        public bool MarkRead(int id)
        {
            var notification = _context.PatientNotifications.Find(id);
            if (notification == null)
                return false;

            notification.IsRead = true;
            _context.SaveChanges();
            return true;
        }

        public int MarkAllReadForPatient(int patientId)
        {
            var unread = _context.PatientNotifications
                .Where(n => n.PatientID == patientId
                            && !n.IsRead
                            && n.NotificationType != PatientNotificationTypes.Operational)
                .ToList();

            foreach (var n in unread)
                n.IsRead = true;

            if (unread.Count > 0)
                _context.SaveChanges();

            return unread.Count;
        }

        public int MarkAllRead(IEnumerable<int> patientIds, string? type = null)
        {
            var ids = patientIds.ToList();
            var query = _context.PatientNotifications
                .Where(n => ids.Contains(n.PatientID) && !n.IsRead);

            if (!string.IsNullOrWhiteSpace(type))
                query = query.Where(n => n.NotificationType == type);

            var unread = query.ToList();
            foreach (var n in unread)
                n.IsRead = true;

            if (unread.Count > 0)
                _context.SaveChanges();

            return unread.Count;
        }
    }

    /// <summary>String constants for PatientNotification.NotificationType.</summary>
    public static class PatientNotificationTypes
    {
        public const string Medication   = "medication";
        public const string Appointment  = "appointment";
        public const string Ultrasound   = "ultrasound";
        public const string Operational  = "operational";
        public const string LabResult    = "lab";
        public const string Prescription = "prescription";
        public const string Note         = "note";
        public const string Message      = "message";
        public const string Community    = "community";
        public const string Pregnancy    = "pregnancy";
        public const string Account      = "account";
    }
}
