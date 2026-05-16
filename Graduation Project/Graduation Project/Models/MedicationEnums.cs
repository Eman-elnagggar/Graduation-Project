namespace Graduation_Project.Models
{
    public enum MedicationSource
    {
        Prescription = 0,
        Self = 1
    }

    public enum MedicationLogStatus
    {
        Scheduled = 0,
        Taken = 1,
        Missed = 2,
        Skipped = 3
    }

    public static class MedicalHistoryEventTypes
    {
        public const string BloodPressureReading = "bp-reading";
        public const string BloodSugar = "blood-sugar";
        public const string LabTest = "lab-test";
        public const string Ultrasound = "ultrasound";
        public const string Appointment = "appointment";
        public const string Alert = "alert";
        public const string DoctorNote = "doctor-note";
        public const string Medication = "medication";
        public const string PregnancyStarted = "pregnancy-started";
        public const string PregnancyEnded = "pregnancy-ended";
        public const string MedicationLog = "medication-log";
    }
}
