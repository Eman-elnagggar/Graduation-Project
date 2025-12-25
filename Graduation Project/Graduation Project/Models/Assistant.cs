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
        public int UserID { get; set; }

        // Navigation
        public virtual Clinic Clinic { get; set; }
        public virtual User User { get; set; }
    }
}
