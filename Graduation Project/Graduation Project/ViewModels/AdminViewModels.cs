using Graduation_Project.Models;
using System.ComponentModel.DataAnnotations;

namespace Graduation_Project.ViewModels
{
    public class AdminDashboardViewModel
    {
        public int TotalPatients { get; set; }
        public int TotalDoctors { get; set; }
        public int TotalAssistants { get; set; }
        public int TotalClinics { get; set; }
        public int AppointmentsToday { get; set; }
        public int PendingClinicInvitations { get; set; }
        public int AlertsToday { get; set; }
        public int PendingDoctorVerifications { get; set; }
        public int ActiveUsers { get; set; }

        public List<int> WeeklyAppointments { get; set; } = new();
        public List<int> MonthlyUserGrowth { get; set; } = new();
        public List<string> MonthLabels { get; set; } = new();

        public List<AdminAlertItem> RecentAlerts { get; set; } = new();
        public List<AdminActivityLog> RecentActivity { get; set; } = new();
    }

    public class AdminAlertItem
    {
        public string Title { get; set; } = "";
        public string Message { get; set; } = "";
        public string AlertType { get; set; } = "";
        public DateTime DateCreated { get; set; }
        public string PatientName { get; set; } = "";
    }

    public class AdminActivityLog
    {
        public string Action { get; set; } = "";
        public string UserName { get; set; } = "";
        public string Role { get; set; } = "";
        public DateTime Timestamp { get; set; }
        public string Icon { get; set; } = "";
        public string Color { get; set; } = "";
    }

    public class AdminUserListViewModel
    {
        public List<AdminUserRow> Users { get; set; } = new();
        public string SearchQuery { get; set; } = "";
        public string RoleFilter { get; set; } = "";
        public string StatusFilter { get; set; } = "";
        public int TotalCount { get; set; }
        public int ActiveCount { get; set; }
        public int InactiveCount { get; set; }
    }

    public class AdminUserRow
    {
        public string UserId { get; set; } = "";
        public string FullName { get; set; } = "";
        public string Email { get; set; } = "";
        public string Role { get; set; } = "";
        public bool IsActive { get; set; }
        public DateTime CreatedDate { get; set; }
        public string? PhoneNumber { get; set; }
        public int? RoleEntityId { get; set; }
    }

    public class AdminDoctorVerificationViewModel
    {
        public List<AdminDoctorVerificationRow> PendingDoctors { get; set; } = new();
        public List<AdminDoctorVerificationRow> ApprovedDoctors { get; set; } = new();
        public List<AdminDoctorVerificationRow> RejectedDoctors { get; set; } = new();
    }

    public class AdminDoctorVerificationRow
    {
        public int DoctorId { get; set; }
        public string UserId { get; set; } = "";
        public string FullName { get; set; } = "";
        public string Email { get; set; } = "";
        public string Specialization { get; set; } = "";
        public string LicenseNumber { get; set; } = "";
        public string LicenseImagePath { get; set; } = "";
        public string VerificationStatus { get; set; } = "";
        public DateTime? VerificationDate { get; set; }
        public string? RejectionNote { get; set; }
        public DateTime CreatedDate { get; set; }
        public string Address { get; set; } = "";
        public List<string> ClinicNames { get; set; } = new();
    }

    public class AdminAssignClinicViewModel
    {
        public int DoctorId { get; set; }
        public string DoctorName { get; set; } = "";
        public List<Clinic> Clinics { get; set; } = new();
        public List<int> AssignedClinicIds { get; set; } = new();
    }

    // ─── Clinic Management ────────────────────────────────────────────────────

    public class AdminClinicsViewModel
    {
        public List<AdminClinicRow> Clinics { get; set; } = new();
        public int TotalClinics { get; set; }
        public int TotalDoctorsAssigned { get; set; }
        public int TotalAssistantsAssigned { get; set; }
        public int TotalAppointments { get; set; }
    }

    public class AdminClinicRow
    {
        public int ClinicID { get; set; }
        public string Name { get; set; } = "";
        public string Location { get; set; } = "";
        public int DoctorCount { get; set; }
        public int AssistantCount { get; set; }
        public int AppointmentCount { get; set; }
    }

    public class AdminClinicFormViewModel
    {
        public int ClinicID { get; set; }

        [Required(ErrorMessage = "Clinic name is required.")]
        [StringLength(200, ErrorMessage = "Name must not exceed 200 characters.")]
        public string Name { get; set; } = "";

        [Required(ErrorMessage = "Location is required.")]
        [StringLength(500, ErrorMessage = "Location must not exceed 500 characters.")]
        public string Location { get; set; } = "";

        public bool IsEdit => ClinicID > 0;
    }

    // ─── Analytics ────────────────────────────────────────────────────────────

    public class AdminAnalyticsViewModel
    {
        public List<int> PatientsPerMonth { get; set; } = new();
        public List<string> PatientMonthLabels { get; set; } = new();

        public List<string> ClinicNames { get; set; } = new();
        public List<int> AppointmentsPerClinic { get; set; } = new();

        public List<string> DoctorNames { get; set; } = new();
        public List<int> DoctorAppointmentCounts { get; set; } = new();

        public List<string> AlertTypes { get; set; } = new();
        public List<int> AlertCounts { get; set; } = new();

        public List<int> SystemGrowth { get; set; } = new();
        public List<string> GrowthMonthLabels { get; set; } = new();

        public int TotalAlerts { get; set; }
        public int TotalAppointments { get; set; }
        public int TotalUsers { get; set; }
        public int TotalClinics { get; set; }
    }
}
