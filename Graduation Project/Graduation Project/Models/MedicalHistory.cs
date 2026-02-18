using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Graduation_Project.Models
{
    public class MedicalHistory
    {
        [Key]
        public int HistoryID { get; set; }

        [ForeignKey("Patient")]
        public int PatientID { get; set; }

        [ForeignKey("Doctor")]
        public int DoctorID { get; set; }

        public int? LabTestID { get; set; } // Nullable FK
        public int? ImageID { get; set; }   // Nullable FK

        public string EventType { get; set; }
        public string Summary { get; set; }
        public DateTime DateRecorded { get; set; }
        public DateTime Date { get; set; }

        // Navigation
        public virtual Patient Patient { get; set; }
        public virtual Doctor Doctor { get; set; }
    }
}
