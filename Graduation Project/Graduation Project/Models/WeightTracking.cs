using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Graduation_Project.Models
{
    public class WeightTracking
    {
        [Key]
        public int WeightTrackingID { get; set; }

        [ForeignKey("Patient")]
        public int PatientID { get; set; }

        public DateTime RecordedDate { get; set; }
        public double WeightKg { get; set; }
        public string Notes { get; set; }

        // Navigation
        public virtual Patient Patient { get; set; }
    }
}
