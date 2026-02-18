using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Graduation_Project.Models
{
    public class Urinalysis_Test
    {
        [Key, ForeignKey("LabTest")]
        public int LabTestID { get; set; }

        public string Color { get; set; }
        public float PH { get; set; }
        public float Specific_Gravity { get; set; }
        public string Protein { get; set; }
        public string Glucose { get; set; }
        public string Nitrite { get; set; }
        public string Ketones { get; set; }
        public string Blood { get; set; }
        public string RBCs { get; set; }
        public string Leukocytes { get; set; }

        // Navigation
        public virtual LabTest LabTest { get; set; }
    }
}
