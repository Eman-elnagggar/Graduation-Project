using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Graduation_Project.Models
{
    public class PatientBloodPressure
    {
        [Key]
        public int ID { get; set; }

        [ForeignKey("Patient")]
        public int PatientID { get; set; }

        public string BloodPressure { get; set; } // e.g. "120/80"
        public DateTime DateTime { get; set; }

        /// <summary>
        /// When the reading was taken, e.g. "Morning", "Evening".
        /// Stored as a string so no enum migration is needed later.
        /// </summary>
        public string? MeasurementTime { get; set; }

        // Navigation
        public virtual Patient Patient { get; set; }
    }
}
