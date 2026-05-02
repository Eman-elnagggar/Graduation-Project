using Graduation_Project.Models;

namespace Graduation_Project.ViewModels
{
    public class AssistantPatientDetailsViewModel
    {
        public Assistant Assistant { get; set; } = null!;
        public string AssistantName { get; set; } = string.Empty;
        public Clinic Clinic { get; set; } = null!;
        public string ClinicName { get; set; } = string.Empty;
        public Patient Patient { get; set; } = null!;
        public string PatientName { get; set; } = "Patient";
        public List<Doctor> AssignedDoctors { get; set; } = new();
        public List<Appointment> Appointments { get; set; } = new();
        public List<PatientDrug> Medications { get; set; } = new();
        public List<PregnancyRecord> PregnancyRecords { get; set; } = new();
    }
}
