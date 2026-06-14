namespace Graduation_Project.Models
{
    public class DoctorNotification
    {
        public int Id { get; set; }
        public int DoctorID { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string NotificationType { get; set; } = string.Empty; // "patient_risk", "invitation_accepted", "admin_approved"
        public DateTime DateCreated { get; set; } = DateTime.Now;
        public bool IsRead { get; set; }
        public string? ActionUrl { get; set; }
        public virtual Doctor Doctor { get; set; } = null!;
    }
}
