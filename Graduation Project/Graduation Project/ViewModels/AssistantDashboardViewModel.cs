using Graduation_Project.Models;

namespace Graduation_Project.ViewModels
{
    public class AssistantDashboardViewModel
    {
        public Assistant Assistant { get; set; } = null!;
        public string AssistantName { get; set; } = string.Empty;
        public DateTime SelectedDate { get; set; } = DateTime.Today;
        public string SelectedScheduleStatus { get; set; } = "Booked";

        // Clinic info
        public Clinic Clinic { get; set; } = null!;
        public string ClinicName { get; set; } = string.Empty;

        // Linked doctors
        public List<AssistantDoctorSummary> Doctors { get; set; } = new();
        public int? SelectedDoctorID { get; set; }
        public string SelectedDoctorName { get; set; } = "All Doctors";

        // Quick stats (aggregated or filtered by selected doctor)
        public int TotalPatients { get; set; }
        public int TodayAppointmentsCount { get; set; }
        public int PendingAlertsCount { get; set; }
        public int TestsThisWeek { get; set; }

        // Today's appointments (all or filtered by selected doctor)
        public List<Appointment> TodaysAppointments { get; set; } = new();

        // Recent unread alerts across clinic patients
        public List<Alert> RecentAlerts { get; set; } = new();

        // Patients assigned to the doctor(s)
        public List<AssistantPatientSummary> Patients { get; set; } = new();
    }

    public class AssistantDoctorSummary
    {
        public int DoctorID { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Specialization { get; set; } = string.Empty;
        public int TodayAppointmentsCount { get; set; }
        public int TotalPatients { get; set; }
    }

    public class AssistantPatientSummary
    {
        public int PatientID { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string DoctorName { get; set; } = string.Empty;
        public int GestationalWeeks { get; set; }
        public string Trimester { get; set; } = string.Empty;
        public DateTime? LastVisitDate { get; set; }
        public bool HasActiveAlerts { get; set; }
    }
}
