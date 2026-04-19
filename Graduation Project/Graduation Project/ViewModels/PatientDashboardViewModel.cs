using Graduation_Project.Models;

namespace Graduation_Project.ViewModels
{
    public class PatientDashboardViewModel
    {
        public Patient Patient { get; set; }

        // User info
        public string UserName { get; set; }

        // Pregnancy progress
        public bool HasActivePregnancy { get; set; }
        public int PregnancyWeek { get; set; }
        public int PregnancyProgressPercent { get; set; }
        public string Trimester { get; set; }
        public string DueDate { get; set; }

        // Health stats
        public string LastBloodPressureValue { get; set; }
        public double LastBloodSugarValue { get; set; }
        public LabTest LastLabTest { get; set; }
        public Appointment NextAppointment { get; set; }
        public List<Appointment> RecentPastAppointments { get; set; } = new();

        // Recent readings for the trackers
        public List<PatientBloodPressure> RecentBloodPressureReadings { get; set; } = new();
        public List<PatientBloodSugar> RecentBloodSugarReadings { get; set; } = new();
        public List<PatientBloodPressure> WeeklyBloodPressureReadings { get; set; } = new();
        public List<PatientBloodSugar> WeeklyBloodSugarReadings { get; set; } = new();

        // Recent activity feed
        public List<RecentActivityItem> RecentActivities { get; set; } = new();

        // Health alerts (critical / dangerous conditions needing attention)
        public List<Alert> HealthAlerts { get; set; } = new();
    }

    public class PatientMessagesViewModel
    {
        public Patient Patient { get; set; } = null!;
        public string UserName { get; set; } = "Patient";
        public List<PatientConversationSummary> Conversations { get; set; } = new();
    }

    public class PatientConversationSummary
    {
        public int ParticipantId { get; set; }
        public string ParticipantType { get; set; } = "Doctor";
        public string ReceiverUserId { get; set; } = string.Empty;
        public string ParticipantName { get; set; } = "Doctor";
        public int UnreadCount { get; set; }
        public DateTime? LastMessageTime { get; set; }
        public string? LastMessagePreview { get; set; }
    }
}
