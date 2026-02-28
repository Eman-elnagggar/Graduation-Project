using Graduation_Project.Models;

namespace Graduation_Project.ViewModels
{
    public class PatientBookAppointmentViewModel
    {
        public Patient Patient { get; set; } = null!;
        public string UserName { get; set; } = string.Empty;
        public List<DoctorBookingInfo> AvailableDoctors { get; set; } = new();
        public int? PreSelectedDoctorId { get; set; }
    }

    public class DoctorBookingInfo
    {
        public int DoctorID { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Specialization { get; set; } = string.Empty;
        public int ClinicID { get; set; }
        public string ClinicName { get; set; } = string.Empty;
        public string ClinicLocation { get; set; } = string.Empty;
        public DateTime? NextAvailableDate { get; set; }
        public TimeSpan? NextAvailableTime { get; set; }
        public List<ClinicInfo> Clinics { get; set; } = new();
    }

    public class ClinicInfo
    {
        public int ClinicID { get; set; }
        public string ClinicName { get; set; } = string.Empty;
        public string ClinicLocation { get; set; } = string.Empty;
    }
}
