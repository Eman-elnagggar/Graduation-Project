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
        public int DoctorID { get; set; }

        [ForeignKey("AIModel")]
        public int? ModelID { get; set; } // Nullable if no AI used

        public DateTime UploadDate { get; set; }
        public string ImagePath { get; set; }
        public string DoctorNotes { get; set; }
        public string AI_AnalysisJSON { get; set; }
        public string TestType { get; set; }

        // Navigation
        public virtual Patient Patient { get; set; }
        public virtual Doctor Doctor { get; set; }
        public virtual AIModel AIModel { get; set; }
    }
}
