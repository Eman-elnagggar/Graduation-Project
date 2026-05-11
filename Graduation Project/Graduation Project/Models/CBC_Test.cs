using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Graduation_Project.Models
{
    public class CBC_Test
    {
        // Assuming 1-to-1 or Inheritance with LabTest
        [Key, ForeignKey("LabTest")]
        public int LabTestID { get; set; }

        public double HB { get; set; }
        public double MCV { get; set; }
        public double MCHC { get; set; }
        public double MCH { get; set; }
        public double RBC_Count { get; set; }
        public double WBC_Count { get; set; }
        public double Platelet_Count { get; set; }
        public double Lymphocytes { get; set; }

        // Navigation
        public virtual LabTest LabTest { get; set; }
    }
}
