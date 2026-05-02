namespace Graduation_Project.ViewModels
{
    public class AdminDoctorListViewModel
    {
        public List<AdminDoctorRow> Doctors { get; set; } = new();
        public string SearchQuery { get; set; } = "";
        public string StatusFilter { get; set; } = "";
        public int TotalCount { get; set; }
        public int ApprovedCount { get; set; }
        public int PendingCount { get; set; }
        public int RejectedCount { get; set; }
        public int BannedCount { get; set; }
    }

    public class AdminDoctorRow
    {
        public int DoctorId { get; set; }
        public string UserId { get; set; } = "";
        public string FullName { get; set; } = "";
        public string Email { get; set; } = "";
        public string OriginalEmail { get; set; } = "";
        public string Specialization { get; set; } = "";
        public string VerificationStatus { get; set; } = "";
        public bool IsActive { get; set; }
        public bool IsBanned { get; set; }
        public DateTime RegisteredDate { get; set; }
        public int PatientCount { get; set; }
        public int AppointmentCount { get; set; }
        public List<string> ClinicNames { get; set; } = new();
    }

    public class AdminDoctorDetailViewModel
    {
        public int DoctorId { get; set; }
        public string UserId { get; set; } = "";
        public string FullName { get; set; } = "";
        public string Email { get; set; } = "";
        public string OriginalEmail { get; set; } = "";
        public string PhoneNumber { get; set; } = "";
        public string Specialization { get; set; } = "";
        public string LicenseNumber { get; set; } = "";
        public string LicenseImagePath { get; set; } = "";
        public string VerificationStatus { get; set; } = "";
        public DateTime? VerificationDate { get; set; }
        public string? RejectionNote { get; set; }
        public string Address { get; set; } = "";
        public DateTime RegisteredDate { get; set; }
        public bool IsActive { get; set; }
        public bool IsBanned { get; set; }
        public DateTimeOffset? LockoutEnd { get; set; }

        public int TotalPatients { get; set; }
        public int TotalAppointments { get; set; }
        public int TotalPrescriptions { get; set; }
        public int TotalNotes { get; set; }
        public int TotalLabTests { get; set; }

        public List<string> ClinicNames { get; set; } = new();
        public List<string> AssistantNames { get; set; } = new();
        public List<AdminDoctorPatientRow> Patients { get; set; } = new();
        public List<AdminDoctorAppointmentRow> RecentAppointments { get; set; } = new();
    }

    public class AdminDoctorPatientRow
    {
        public int PatientId { get; set; }
        public string FullName { get; set; } = "";
        public string Email { get; set; } = "";
        public string Status { get; set; } = "";
        public DateTime JoinDate { get; set; }
    }

    public class AdminDoctorAppointmentRow
    {
        public int AppointmentId { get; set; }
        public string PatientName { get; set; } = "";
        public DateTime Date { get; set; }
        public TimeSpan Time { get; set; }
        public bool IsBooked { get; set; }
        public string ClinicName { get; set; } = "";
    }
}
