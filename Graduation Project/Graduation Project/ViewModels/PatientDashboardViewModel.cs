using Graduation_Project.Models;

namespace Graduation_Project.ViewModels
{
    public class PatientDashboardViewModel
    {
        public Patient Patient { get; set; }

        // User info
        public string UserName { get; set; }

        // Pregnancy progress
        public int PregnancyWeek { get; set; }
        public int PregnancyProgressPercent { get; set; }
        public string Trimester { get; set; }
        public string DueDate { get; set; }

        // Health stats
        public string LastBloodPressureValue { get; set; }
        public double LastBloodSugarValue { get; set; }
        public LabTest LastLabTest { get; set; }
        public Appointment NextAppointment { get; set; }

        // Recent readings for the trackers
        public List<PatientBloodPressure> RecentBloodPressureReadings { get; set; } = new();
        public List<PatientBloodSugar> RecentBloodSugarReadings { get; set; } = new();

        // Recent activity feed
        public List<RecentActivityItem> RecentActivities { get; set; } = new();

        // Health alerts (critical / dangerous conditions needing attention)
        public List<Alert> HealthAlerts { get; set; } = new();
    }
}
