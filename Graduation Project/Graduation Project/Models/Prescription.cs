using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Graduation_Project.Models
{
    public class Prescription
    {
        [Key]
        public int PrescriptionID { get; set; }

        [ForeignKey("Doctor")]
        public int DoctorID { get; set; }

        [ForeignKey("Patient")]
        public int PatientID { get; set; }

        public DateTime PrescriptionDate { get; set; }

        // General notes on the prescription as a whole
        public string Notes { get; set; }

        // Navigation
        public virtual Doctor Doctor { get; set; }
        public virtual Patient Patient { get; set; }

        // One prescription has many medicine items
        public virtual ICollection<PrescriptionItem> Items { get; set; }
    }
}
