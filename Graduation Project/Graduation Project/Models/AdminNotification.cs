namespace Graduation_Project.Models
{
    // Platform-level notifications for administrators. Unlike DoctorNotification and
    // PatientNotification there is no owner foreign key: these describe the system itself,
    // so every admin sees the same feed.
    public class AdminNotification
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;

        // "doctor_registered", "assistant_registered", "clinic_created"
        public string NotificationType { get; set; } = string.Empty;

        // Drives the icon and colour on the dashboard: "info", "warning", "danger", "success"
        public string Severity { get; set; } = "info";

        public DateTime DateCreated { get; set; } = DateTime.Now;
        public bool IsRead { get; set; }
        public string? ActionUrl { get; set; }
    }
}
