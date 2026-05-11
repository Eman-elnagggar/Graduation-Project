using Graduation_Project.Models;

namespace Graduation_Project.ViewModels.Ultrasound
{
    public class PatientUltrasoundHistoryViewModel
    {
        public Patient Patient { get; set; } = null!;
        public string PatientName { get; set; } = "Patient";
        public List<UltrasoundImage> DoctorScans { get; set; } = new();
        public List<UltrasoundImage> SelfScans { get; set; } = new();
    }
}
