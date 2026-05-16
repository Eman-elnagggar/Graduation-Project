using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Graduation_Project.Models
{
    public class LabTest
    {
        [Key]
        public int LabTestID { get; set; }

        [ForeignKey("Patient")]
        public int PatientID { get; set; }

        [ForeignKey("Doctor")]
        public int? DoctorID { get; set; }

        [ForeignKey("AIModel")]
        public int? ModelID { get; set; } // Nullable if no AI used

        [ForeignKey("TestReport")]
        public int? ReportID { get; set; } // Nullable — set once test is included in a report

        public DateTime UploadDate { get; set; }
        public string? ImagePath { get; set; }
        public string? AI_AnalysisJSON { get; set; } // Per-test AI raw JSON result
        public string? TestName { get; set; }
        public string? OcrRawJson { get; set; }
        public string? OcrNormalizedJson { get; set; }
        public string? ConfirmedJson { get; set; }
        public string? AnalysisStatus { get; set; }
        public string TestType { get; set; }

        // Navigation
        public virtual Patient Patient { get; set; }
        public virtual Doctor Doctor { get; set; }
        public virtual AIModel AIModel { get; set; }
        public virtual TestReport TestReport { get; set; }
    }
}
