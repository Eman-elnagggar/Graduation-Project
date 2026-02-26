using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Graduation_Project.Models
{
    public class HBsAg_Test
    {
        [Key, ForeignKey("LabTest")]
        public int LabTestID { get; set; }

        public string HBsAg { get; set; }

        // Navigation
        public virtual LabTest LabTest { get; set; }
    }
}
