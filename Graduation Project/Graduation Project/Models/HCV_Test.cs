using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Graduation_Project.Models
{
    public class HCV_Test
    {
        [Key, ForeignKey("LabTest")]
        public int LabTestID { get; set; }

        public string HCV { get; set; }

        // Navigation
        public virtual LabTest LabTest { get; set; }
    }
}
