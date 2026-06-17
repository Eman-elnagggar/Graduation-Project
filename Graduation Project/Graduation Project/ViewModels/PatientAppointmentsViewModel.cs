using Graduation_Project.Models;

namespace Graduation_Project.ViewModels
{
    public class PatientAppointmentsViewModel
    {
        public Patient Patient { get; set; } = null!;
        public string UserName { get; set; } = string.Empty;
        public List<Appointment> UpcomingAppointments { get; set; } = new();
        public List<Appointment> PastAppointments { get; set; } = new();
        public List<PatientDoctor> MyDoctors { get; set; } = new();
        public PatientDoctor? PrimaryDoctor { get; set; }
        public int UnreadAlertsCount { get; set; }

        // Pregnancy journey context (mirrors the patient dashboard)
        public bool HasActivePregnancy { get; set; }
        public int PregnancyWeek { get; set; }
        public int PregnancyProgressPercent { get; set; }
        public string Trimester { get; set; } = "Not Active";
        public DateTime? DueDate { get; set; }
    }
}
