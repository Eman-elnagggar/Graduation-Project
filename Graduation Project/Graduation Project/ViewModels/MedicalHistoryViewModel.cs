using Graduation_Project.Models;

namespace Graduation_Project.ViewModels
{
    public class MedicalHistoryViewModel
    {
        public Patient Patient { get; set; } = null!;
        public string UserName { get; set; } = "Patient";

        // ?? flat, sorted timeline ????????????????????????????????????????
        public List<MedicalHistoryEntry> TimelineEntries { get; set; } = new();

        // ?? quick-stat counts ????????????????????????????????????????????
        public int LabTestCount { get; set; }
        public int UltrasoundCount { get; set; }
        public int BloodPressureCount { get; set; }
        public int BloodSugarCount { get; set; }
        public int DoctorNoteCount { get; set; }
        public int MedicationCount { get; set; }
    }

    /// <summary>
    /// One flattened timeline entry built from any clinical record.
    /// EventType values: bp-reading | blood-sugar | lab-test | ultrasound |
    ///                   doctor-note | medication | pregnancy-started | pregnancy-ended
    /// Status values:    normal | attention | critical
    /// </summary>
    public class MedicalHistoryEntry
    {
        public DateTime DateTime { get; set; }
        public string EventType { get; set; } = "";
        public string Status { get; set; } = "normal";
        public string Title { get; set; } = "";
        public string? SubTitle { get; set; }
        public string? DoctorName { get; set; }
        public string? ClinicName { get; set; }

        // ?? typed payloads (at most one is non-null) ?????????????????????
        public PatientBloodPressure? BloodPressure { get; set; }
        public PatientBloodSugar? BloodSugar { get; set; }
        public LabTest? LabTest { get; set; }
        public UltrasoundImage? Ultrasound { get; set; }
        public Note? DoctorNote { get; set; }
        public Prescription? Prescription { get; set; }
    }
}
