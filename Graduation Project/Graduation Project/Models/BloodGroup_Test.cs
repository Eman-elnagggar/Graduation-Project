using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Graduation_Project.Models
{
    public class BloodGroup_Test
    {
        [Key, ForeignKey("LabTest")]
        public int LabTestID { get; set; }

        public string ABO_Group { get; set; }
        public string RH_Factor { get; set; }

        // Navigation
        public virtual LabTest LabTest { get; set; }
    }
}
