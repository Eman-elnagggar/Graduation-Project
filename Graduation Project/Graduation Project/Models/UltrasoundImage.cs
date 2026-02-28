using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Graduation_Project.Models
{
    public class UltrasoundImage
    {
        [Key]
        public int ImageID { get; set; }

        [ForeignKey("Patient")]
        public int PatientID { get; set; }

        [ForeignKey("AIModel")]
        public int? ModelID { get; set; }

        [ForeignKey("Doctor")]
        public int? DoctorID { get; set; }

        public string ImagePath { get; set; }
        public DateTime UploadDate { get; set; }
        public string DetectedAnomaly { get; set; }
        public string DoctorComments { get; set; }
        public string AI_Result_JSON { get; set; }
        public double? Lymphocytes { get; set; } // Included based on diagram

        // Navigation
        public virtual Patient Patient { get; set; }
        public virtual AIModel AIModel { get; set; }
        public virtual Doctor Doctor { get; set; }
    }
}
