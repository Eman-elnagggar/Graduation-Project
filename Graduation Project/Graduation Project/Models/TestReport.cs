using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Graduation_Project.Models
{
    public class TestReport
    {
        [Key]
        public int ReportID { get; set; }

        [ForeignKey("Patient")]
        public int PatientID { get; set; }

        [ForeignKey("Doctor")]
        public int DoctorID { get; set; }

        public DateTime ReportDate { get; set; }

        // Overall AI verdict e.g. "Normal", "Abnormal", "Requires Attention"
        public string OverallStatus { get; set; }

        // AI confidence score e.g. 96.5
        public double? ConfidenceScore { get; set; }

        // AI-generated narrative summary shown in the "Analysis Summary" section
        public string AISummary { get; set; }

        // Optional doctor interpretation / remarks
        public string DoctorInterpretation { get; set; }

        // Navigation
        public virtual Patient Patient { get; set; }
        public virtual Doctor Doctor { get; set; }

        // One report covers many lab tests
        public virtual ICollection<LabTest> LabTests { get; set; }
    }
}
