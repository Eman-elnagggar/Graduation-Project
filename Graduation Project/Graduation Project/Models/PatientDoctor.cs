using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Graduation_Project.Models
{
    // Many-to-Many Relationship Table
    public class PatientDoctor
    {
        [Key, Column(Order = 0)]
        [ForeignKey("Doctor")]
        public int DoctorID { get; set; }

        [Key, Column(Order = 1)]
        [ForeignKey("Patient")]
        public int PatientID { get; set; }

        public string Status { get; set; }
        public DateTime RequestDate { get; set; }
        public DateTime? ResponseDate { get; set; }
        public bool IsPrimary { get; set; }

        // Navigation
        public virtual Doctor Doctor { get; set; }
        public virtual Patient Patient { get; set; }
    }
}
