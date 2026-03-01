using Graduation_Project.Models;

namespace Graduation_Project.ViewModels
{
    public class AssistantAvailabilityViewModel
    {
        public Assistant Assistant { get; set; } = null!;
        public string AssistantName { get; set; } = string.Empty;
        public Clinic Clinic { get; set; } = null!;
        public string ClinicName { get; set; } = string.Empty;
        public List<AssistantDoctorSummary> Doctors { get; set; } = new();
        public int? SelectedDoctorID { get; set; }
    }

    public class QuickSetupRequest
    {
        public int DoctorId { get; set; }
        public List<int> WorkingDays { get; set; } = new();
        public string StartTime { get; set; } = "09:00";
        public string EndTime { get; set; } = "17:00";
        public int SlotDuration { get; set; } = 30;
        public int WeeksAhead { get; set; } = 2;
    }
}
