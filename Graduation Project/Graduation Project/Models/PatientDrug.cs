using Graduation_Project.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Graduation_Project.Models
{
    public class PatientDrug
    {
        [Key]
        public int DrugID { get; set; }

        [ForeignKey("Patient")]
        public int PatientID { get; set; }

        public string DrugName { get; set; }
        public int DurationMonths { get; set; }
        public string Reason { get; set; }
        public double DoseMgPerDay { get; set; }

        // Navigation
        public virtual Patient Patient { get; set; }
    }
}
