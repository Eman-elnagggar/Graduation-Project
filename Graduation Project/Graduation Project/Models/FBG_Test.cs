using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Graduation_Project.Models
{
    public class FBG_Test
    {
        [Key, ForeignKey("LabTest")]
        public int LabTestID { get; set; }

        public float FBG { get; set; }

        // Navigation
        public virtual LabTest LabTest { get; set; }
    }
}
