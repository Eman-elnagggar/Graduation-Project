using Graduation_Project.Models;

namespace Graduation_Project.ViewModels.Ultrasound
{
    public class UltrasoundResultViewModel
    {
        public UltrasoundImage Ultrasound { get; set; } = null!;
        public string PatientName { get; set; } = "Patient";
        public string DoctorName { get; set; } = "Doctor";
    }
}
