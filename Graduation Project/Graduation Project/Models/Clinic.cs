using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Graduation_Project.Models
{
    public class Clinic
    {
        [Key]
        public int ClinicID { get; set; }

        [ForeignKey("Doctor")]
        public int DoctorID { get; set; }

        public string Name { get; set; }
        public string Location { get; set; }

        // Navigation
        public virtual Doctor Doctor { get; set; }
    }
}
