using Graduation_Project.Models;

namespace Graduation_Project.Interfaces
{
    public interface IPatientNotificationService
    {
        /// <summary>
        /// Persists a patient notification, fires a web-push, and returns the number
        /// created (0 if skipped by same-day deduplication). Synchronous to match the
        /// existing Alert/repository pipeline.
        /// </summary>
        int Notify(int patientId, string title, string message, string type,
                   string? actionUrl = null, bool dedupePerDay = false, string severity = "info",
                   bool sendPush = true);

        /// <summary>Patient-facing notifications (excludes "operational"), newest first.</summary>
        List<PatientNotification> GetForPatient(int patientId, int take = 50);

        /// <summary>Notifications for many patients, optionally filtered to a single type.</summary>
        List<PatientNotification> GetForPatients(IEnumerable<int> patientIds, string? type = null);

        bool MarkRead(int id);

        /// <summary>Marks all unread patient-facing notifications (excludes "operational") read.</summary>
        int MarkAllReadForPatient(int patientId);

        /// <summary>Marks all unread notifications for the given patients, optionally filtered to one type.</summary>
        int MarkAllRead(IEnumerable<int> patientIds, string? type = null);
    }
}
