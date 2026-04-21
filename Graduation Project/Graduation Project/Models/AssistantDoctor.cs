using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Graduation_Project.Models
{
    // Many-to-Many Relationship Table
    public class AssistantDoctor
    {
        [Key, Column(Order = 0)]
        [ForeignKey("Assistant")]
        public int AssistantID { get; set; }

        [Key, Column(Order = 1)]
        [ForeignKey("Doctor")]
        public int DoctorID { get; set; }

        // Navigation
        public virtual Assistant Assistant { get; set; }
        public virtual Doctor Doctor { get; set; }
    }
}
