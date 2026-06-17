using Graduation_Project.Models;

namespace Graduation_Project.ViewModels
{
    public class AssistantPatientsViewModel
    {
        public Assistant Assistant { get; set; } = null!;
        public string AssistantName { get; set; } = string.Empty;
        public Clinic Clinic { get; set; } = null!;
        public string ClinicName { get; set; } = string.Empty;
        public List<AssistantDoctorSummary> Doctors { get; set; } = new();
        public int? SelectedDoctorID { get; set; }
        public string SelectedDoctorName { get; set; } = "All Doctors";
        public List<AssistantPatientAppointmentsSummary> Patients { get; set; } = new();
    }

    public class AssistantPatientAppointmentsSummary
    {
        public int PatientID { get; set; }
        public string FullName { get; set; } = "Patient";
        public string? PhoneNumber { get; set; }
        public int DoctorID { get; set; }
        public string DoctorName { get; set; } = "Doctor";
        public string DoctorSpecialization { get; set; } = string.Empty;
        public List<Appointment> Appointments { get; set; } = new();
    }
}
