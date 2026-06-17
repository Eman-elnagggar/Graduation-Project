using System.Collections.Generic;
using Graduation_Project.Models;

namespace Graduation_Project.ViewModels.Ultrasound
{
    public class UltrasoundHistoryViewModel
    {
        public int DoctorId { get; set; }
        public Patient Patient { get; set; } = null!;
        public string PatientName { get; set; } = "Patient";
        public List<UltrasoundImage> Scans { get; set; } = new();
    }
}
