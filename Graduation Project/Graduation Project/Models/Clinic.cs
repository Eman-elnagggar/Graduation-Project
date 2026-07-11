using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Graduation_Project.Models
{
    public class Clinic
    {
        [Key]
        public int ClinicID { get; set; }

        public string Name { get; set; }
        public string Location { get; set; }

        // The doctor who created the clinic; acts as its admin (manages members).
        // Nullable so clinics created before ownership existed keep working.
        [ForeignKey(nameof(Owner))]
        public int? OwnerDoctorID { get; set; }

        // Navigation
        public virtual Doctor? Owner { get; set; }
        public virtual ICollection<ClinicDoctor> ClinicDoctors { get; set; }
        public virtual ICollection<Assistant> Assistants { get; set; }
    }
}
