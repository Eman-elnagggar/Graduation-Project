using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Graduation_Project.Models
{
    public class Assistant
    {
        [Key]
        public int AssistantID { get; set; }

        [ForeignKey("Clinic")]
        public int ClinicID { get; set; }

        [ForeignKey("User")]
        public string UserID { get; set; }

        // Navigation
        public virtual Clinic Clinic { get; set; }
        public virtual ApplicationUser User { get; set; }
        public virtual ICollection<AssistantDoctor> AssistantDoctors { get; set; }
    }
}
