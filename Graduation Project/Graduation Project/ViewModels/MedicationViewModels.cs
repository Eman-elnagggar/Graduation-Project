using Graduation_Project.Models;
using Graduation_Project.Services;

namespace Graduation_Project.ViewModels
{
    public class PatientMedicationIndexViewModel
    {
        public Patient Patient { get; set; } = null!;
        public string UserName { get; set; } = "Patient";
        public List<Medication> ActiveMedications { get; set; } = new();
        public int GlobalLeadTimeMinutes { get; set; }
        public List<MedicationDueSlot> TodaySlots { get; set; } = new();
        public List<DailyAdherencePoint> WeeklyChartData { get; set; } = new();
    }

    public class PatientMedicationDailyViewModel
    {
        public Patient Patient { get; set; } = null!;
        public string UserName { get; set; } = "Patient";
        public DateTime Date { get; set; }
        public List<MedicationDueSlot> DueSlots { get; set; } = new();
    }

    public class PatientMedicationAddViewModel
    {
        public Patient Patient { get; set; } = null!;
        public string UserName { get; set; } = "Patient";
    }

    public class PatientMedicationEditViewModel
    {
        public Patient Patient { get; set; } = null!;
        public string UserName { get; set; } = "Patient";
        public Medication Medication { get; set; } = null!;
    }

    public class PatientMedicationHistoryViewModel
    {
        public Patient Patient { get; set; } = null!;
        public string UserName { get; set; } = "Patient";
        public Medication Medication { get; set; } = null!;
        public List<MedicationLog> Logs { get; set; } = new();
        public MedicationAdherenceSummary Summary { get; set; } = new();
    }

    public class DoctorMedicationSummaryViewModel
    {
        public Doctor Doctor { get; set; } = null!;
        public Patient Patient { get; set; } = null!;
        public string DoctorName { get; set; } = "Doctor";
        public string PatientName { get; set; } = "Patient";
        public MedicationAdherenceSummary Summary { get; set; } = new();
        public List<MedicationLog> RecentLogs { get; set; } = new();
    }

    public class DailyAdherencePoint
    {
        public DateTime Date { get; set; }
        public int Taken { get; set; }
        public int Missed { get; set; }
        public int Skipped { get; set; }
    }
}
