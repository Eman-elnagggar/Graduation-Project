using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Graduation_Project.Models
{
    // Many-to-Many Relationship Table
    public class ClinicDoctor
    {
        [Key, Column(Order = 0)]
        [ForeignKey("Clinic")]
        public int ClinicID { get; set; }

        [Key, Column(Order = 1)]
        [ForeignKey("Doctor")]
        public int DoctorID { get; set; }

        // Navigation
        public virtual Clinic Clinic { get; set; }
        public virtual Doctor Doctor { get; set; }
    }
}
