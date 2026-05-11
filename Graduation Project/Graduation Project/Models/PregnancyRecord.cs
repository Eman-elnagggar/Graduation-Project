using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Graduation_Project.Models
{
    public class PregnancyRecord
    {
        [Key]
        public int PregnancyRecordID { get; set; }

        [ForeignKey("Patient")]
        public int PatientID { get; set; }

        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public virtual Patient Patient { get; set; }
    }
}
