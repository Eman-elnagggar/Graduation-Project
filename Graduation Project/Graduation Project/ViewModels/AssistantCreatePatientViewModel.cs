using Graduation_Project.Models;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;

namespace Graduation_Project.ViewModels
{
    public class AssistantCreatePatientViewModel
    {
        // Required Fields
        [Required(ErrorMessage = "First name is required")]
        [StringLength(50)]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Last name is required")]
        [StringLength(50)]
        public string LastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        [StringLength(256)]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone number is required")]
        [StringLength(20)]
        [RegularExpression(@"^[\d\s\-\+\(\)]+$", ErrorMessage = "Invalid phone number format")]
        public string PhoneNumber { get; set; } = string.Empty;

        // Optional Health Information
        [DataType(DataType.Date)]
        public DateTime? DateOfBirth { get; set; }

        [StringLength(500)]
        public string Address { get; set; } = string.Empty;

        [Range(0, 300)]
        public double? WeightKg { get; set; }

        [Range(0, 250)]
        public double? HeightCm { get; set; }

        // Health Habits
        public bool BloodPressureIssue { get; set; } = false;
        public bool Smoking { get; set; } = false;
        public bool AlcoholUse { get; set; } = false;

        // Pregnancy Info (if applicable)
        public bool IsPregnant { get; set; } = false;

        [DataType(DataType.Date)]
        public DateTime? PregnancyDate { get; set; }

        [Range(0, 40)]
        public int? GestationalWeeks { get; set; }

        // UI-only — never posted from form, must not be validated
        [ValidateNever] public Assistant Assistant { get; set; } = null!;
        public string AssistantName { get; set; } = string.Empty;
        [ValidateNever] public Clinic Clinic { get; set; } = null!;
        public string ClinicName { get; set; } = string.Empty;
        [ValidateNever] public List<AssistantDoctorSummary> Doctors { get; set; } = new();

        [Required(ErrorMessage = "Please select a doctor")]
        public int? SelectedDoctorID { get; set; }

        // Error Messages
        public string? ErrorMessage { get; set; }
        public string? SuccessMessage { get; set; }
    }
}
