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

        // Navigation
        public virtual ICollection<ClinicDoctor> ClinicDoctors { get; set; }
        public virtual ICollection<Assistant> Assistants { get; set; }
    }
}
