using Graduation_Project.Models;

namespace Graduation_Project.ViewModels
{
    public class DoctorDashboardViewModel
    {
        public Doctor Doctor { get; set; } = null!;
        public string DoctorName { get; set; } = "Doctor";

        public int TodayAppointmentsCount { get; set; }
        public int ThisWeekAppointmentsCount { get; set; }
        public int ActivePatientsCount { get; set; }
        public int NewPatientsThisMonth { get; set; }
        public int HighRiskPatientsCount { get; set; }
        public int MediumRiskPatientsCount { get; set; }
        public int LowRiskPatientsCount { get; set; }
        public int UnreadMessagesCount { get; set; }
        public int UrgentMessagesCount { get; set; }
        public int UnreadAlertsCount { get; set; }

        public List<int> WeeklyAppointmentCounts { get; set; } = new();

        public Appointment? NextAppointment { get; set; }
        public List<Appointment> TodayAppointments { get; set; } = new();
        public List<Alert> RecentAlerts { get; set; } = new();
        public List<DoctorPatientSummary> PriorityPatients { get; set; } = new();
        public List<DoctorDashboardRecentMessageSummary> RecentMessages { get; set; } = new();
    }

    public class DoctorDashboardRecentMessageSummary
    {
        public int PatientId { get; set; }
        public string PatientName { get; set; } = "Patient";
        public string LastMessagePreview { get; set; } = "Start a conversation";
        public DateTime? LastMessageTime { get; set; }
        public int UnreadCount { get; set; }
    }

    public class DoctorPatientsViewModel
    {
        public Doctor Doctor { get; set; } = null!;
        public string DoctorName { get; set; } = "Doctor";
        public List<DoctorPatientSummary> Patients { get; set; } = new();
    }

    public class DoctorPatientSummary
    {
        public int PatientID { get; set; }
        public ApplicationUser? User { get; set; }
        public int GestationalAge { get; set; }
        public string? RiskLevel { get; set; }
        public bool NeedsAttention { get; set; }
        public string? BloodType { get; set; }
        public DateTime? NextAppointmentDate { get; set; }
        public string? LastBloodPressure { get; set; }
        public double? LastBloodSugar { get; set; }
        public double? LastWeightKg { get; set; }
        public DateTime? LastVisitDate { get; set; }
    }

    public class DoctorMessagesViewModel
    {
        public Doctor Doctor { get; set; } = null!;
        public string DoctorName { get; set; } = "Doctor";
        public List<DoctorConversationSummary> Conversations { get; set; } = new();
    }

    public class DoctorConversationSummary
    {
        public int PatientId { get; set; }
        public string ReceiverUserId { get; set; } = string.Empty;
        public string PatientName { get; set; } = "Patient";
        public int UnreadCount { get; set; }
        public DateTime? LastMessageTime { get; set; }
        public string? LastMessagePreview { get; set; }
    }

    public class DoctorScheduleViewModel
    {
        public Doctor Doctor { get; set; } = null!;
        public string DoctorName { get; set; } = "Doctor";
        public List<Appointment> Appointments { get; set; } = new();
    }

    public class DoctorClinicTeamViewModel
    {
        public Doctor Doctor { get; set; } = null!;
        public string DoctorName { get; set; } = "Doctor";
        public List<Assistant> Assistants { get; set; } = new();
        public List<PendingInvitationViewModel> PendingInvitations { get; set; } = new();
    }

    public class PendingInvitationViewModel
    {
        public int InvitationID { get; set; }
        public string Email { get; set; } = string.Empty;
        public DateTime SentAt { get; set; }
    }

    public class DoctorAnalyticsViewModel
    {
        public Doctor Doctor { get; set; } = null!;
        public string DoctorName { get; set; } = "Doctor";

        public int ActivePatientsCount { get; set; }
        public int NewPatientsThisMonth { get; set; }
        public int CompletedAppointmentsCount { get; set; }
        public int AppointmentsTrend { get; set; }
        public int CompletionRate { get; set; }
        public int HighRiskPatientsCount { get; set; }
        public int LowRiskCount { get; set; }
        public int MediumRiskCount { get; set; }
        public List<int> WeeklyAppointmentCounts { get; set; } = new();
        public int[] TrimesterCounts { get; set; } = new[] { 0, 0, 0 };
        public List<DoctorRecentLabTestSummary> RecentLabTests { get; set; } = new();
    }

    public class DoctorRecentLabTestSummary
    {
        public Patient? Patient { get; set; }
        public string TestType { get; set; } = string.Empty;
        public DateTime UploadDate { get; set; }
        public bool IsReviewed { get; set; }
    }

    public class DoctorProfileViewModel
    {
        public Doctor Doctor { get; set; } = null!;
        public string DoctorName { get; set; } = "Doctor";
        public string? ClinicName { get; set; }
        public string? ClinicAddress { get; set; }
        public decimal ConsultationFee { get; set; }
        public string? WorkingHours { get; set; }
        public string? Languages { get; set; }
        public int YearsOfExperience { get; set; }
        public string? Education { get; set; }
        public string? ClinicPhone { get; set; }

        public int TotalPatientsEver { get; set; }
        public int TotalAppointments { get; set; }
        public double AverageRating { get; set; }
        public int SatisfactionRate { get; set; }
    }

    public class DoctorPatientDetailsViewModel
    {
        public Doctor Doctor { get; set; } = null!;
        public string DoctorName { get; set; } = "Doctor";
        public Patient Patient { get; set; } = null!;

        public string? RiskLevel { get; set; }
        public DateTime? ExpectedDeliveryDate { get; set; }

        public string? LastBloodPressure { get; set; }
        public DateTime? LastBPDate { get; set; }
        public double LastBloodSugar { get; set; }
        public DateTime? LastBSDate { get; set; }

        public Appointment? NextAppointment { get; set; }
        public List<PatientBloodPressure> BloodPressureHistory { get; set; } = new();
        public List<LabTest> LabTests { get; set; } = new();
        public List<Appointment> AppointmentHistory { get; set; } = new();
        public List<Note> ClinicalNotes { get; set; } = new();
        public List<Prescription> Prescriptions { get; set; } = new();
    }
}
