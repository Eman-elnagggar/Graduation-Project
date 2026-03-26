using Graduation_Project.Models;

namespace Graduation_Project.ViewModels
{
    public class AssistantAppointmentsViewModel
    {
        public Assistant Assistant { get; set; } = null!;
        public string AssistantName { get; set; } = string.Empty;
        public Clinic Clinic { get; set; } = null!;
        public string ClinicName { get; set; } = string.Empty;
        public DateTime SelectedDate { get; set; } = DateTime.Today;
        public List<AssistantDoctorSummary> Doctors { get; set; } = new();
        public int? SelectedDoctorID { get; set; }
    }
}
