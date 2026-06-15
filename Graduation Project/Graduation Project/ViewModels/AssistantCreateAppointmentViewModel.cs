using Graduation_Project.Models;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;

namespace Graduation_Project.ViewModels
{
    public class AssistantCreateAppointmentViewModel
    {
        // Appointment Details
        [Required(ErrorMessage = "Please select a doctor")]
        public int? DoctorID { get; set; }

        [Required(ErrorMessage = "Date is required")]
        [DataType(DataType.Date)]
        public DateTime? AppointmentDate { get; set; }

        [Required(ErrorMessage = "Time is required")]
        [DataType(DataType.Time)]
        public TimeSpan? AppointmentTime { get; set; }

        [StringLength(500)]
        public string Reason { get; set; } = string.Empty;

        [StringLength(1000)]
        public string Notes { get; set; } = string.Empty;

        // Patient Selection Mode: Existing or New
        [Required(ErrorMessage = "Please select a patient option")]
        public string PatientOption { get; set; } = "existing"; // "existing" or "new"

        // For Existing Patient
        public int? ExistingPatientID { get; set; }

        // For New Patient
        [StringLength(50)]
        public string NewPatientFirstName { get; set; } = string.Empty;

        [StringLength(50)]
        public string NewPatientLastName { get; set; } = string.Empty;

        [EmailAddress]
        [StringLength(256)]
        public string NewPatientEmail { get; set; } = string.Empty;

        [StringLength(20)]
        [RegularExpression(@"^[\d\s\-\+\(\)]+$", ErrorMessage = "Invalid phone number format")]
        public string NewPatientPhoneNumber { get; set; } = string.Empty;

        [DataType(DataType.Date)]
        public DateTime? NewPatientDateOfBirth { get; set; }

        [StringLength(500)]
        public string NewPatientAddress { get; set; } = string.Empty;

        // UI-only — never posted from form, must not be validated
        [ValidateNever] public Assistant Assistant { get; set; } = null!;
        public string AssistantName { get; set; } = string.Empty;
        [ValidateNever] public Clinic Clinic { get; set; } = null!;
        public string ClinicName { get; set; } = string.Empty;
        [ValidateNever] public List<AssistantDoctorSummary> Doctors { get; set; } = new();
        [ValidateNever] public List<AssistantPatientAppointmentsSummary> ExistingPatients { get; set; } = new();

        // Messages
        public string? ErrorMessage { get; set; }
        public string? SuccessMessage { get; set; }
        public int? CreatedAppointmentID { get; set; }
        public string? PatientName { get; set; }
    }
}
