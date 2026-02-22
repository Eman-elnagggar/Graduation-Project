using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Graduation_Project.Models
{
    public class PrescriptionItem
    {
        [Key]
        public int ItemID { get; set; }

        [ForeignKey("Prescription")]
        public int PrescriptionID { get; set; }

        public string MedicineName { get; set; }
        public string Dosage { get; set; }          // e.g. "500mg"
        public string Frequency { get; set; }       // e.g. "Twice daily"
        public int DurationDays { get; set; }       // e.g. 7
        public string Instructions { get; set; }    // e.g. "Take after meals"

        // Navigation
        public virtual Prescription Prescription { get; set; }
    }
}
