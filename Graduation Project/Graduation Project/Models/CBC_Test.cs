using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Graduation_Project.Models
{
    public class CBC_Test
    {
        // Assuming 1-to-1 or Inheritance with LabTest
        [Key, ForeignKey("LabTest")]
        public int LabTestID { get; set; }

        public float HB { get; set; }
        public float RBCs_Count { get; set; }
        public float MCV { get; set; }
        public float MCH { get; set; }
        public float MCHC { get; set; }
        public float WBC { get; set; }
        public float lymphocytes { get; set; }
        public float platelet_count { get; set; }

        // Navigation
        public virtual LabTest LabTest { get; set; }
    }
}
