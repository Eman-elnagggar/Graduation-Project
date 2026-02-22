using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Graduation_Project.Models
{
    public class TSH_Test
    {
        [Key, ForeignKey("LabTest")]
        public int LabTestID { get; set; }

        public float TSH { get; set; }
        public string TSH_Unit { get; set; }

        // Navigation
        public virtual LabTest LabTest { get; set; }
    }
}
