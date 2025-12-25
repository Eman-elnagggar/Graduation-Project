using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Graduation_Project.Models
{
    public class Place // Renamed from "Places" for singular convention
    {
        [Key]
        public int PlaceID { get; set; }

        [ForeignKey("Patient")]
        public int PatientID { get; set; }

        public string Name { get; set; }
        public string Type { get; set; }
        public string Address { get; set; }
        public string Phone { get; set; }
        public string ImageURL { get; set; }

        // Navigation
        public virtual Patient Patient { get; set; }
    }
}
