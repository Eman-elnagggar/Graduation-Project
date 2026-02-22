using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Graduation_Project.Models
{
    public class PatientBloodSugar
    {
        [Key]
        public int ID { get; set; }

        [ForeignKey("Patient")]
        public int PatientID { get; set; }

        public double BloodSugar { get; set; }
        public DateTime DateTime { get; set; }

        /// <summary>
        /// When the reading was taken, e.g. "Fasting (Before breakfast)", "After Lunch (2 hours)".
        /// </summary>
        public string? MeasurementTime { get; set; }

        // Navigation
        public virtual Patient Patient { get; set; }
    }
}
