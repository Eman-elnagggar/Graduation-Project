using System.Collections.Generic;
using Graduation_Project.Models;
using Microsoft.AspNetCore.Http;

namespace Graduation_Project.ViewModels.Ultrasound
{
    public class UltrasoundUploadViewModel
    {
        public int DoctorId { get; set; }
        public int PatientId { get; set; }
        public IFormFile? ImageFile { get; set; }
        public List<Patient> Patients { get; set; } = new();

        /// <summary>
        /// When true, the image is sent to the AI for analysis.
        /// When false, the image is saved with DoctorNote only (no AI call).
        /// </summary>
        public bool AnalyzeWithAI { get; set; } = true;

        /// <summary>
        /// Optional doctor note attached to the scan. Used as DoctorComments on the record.
        /// </summary>
        public string? DoctorNote { get; set; }
    }
}
