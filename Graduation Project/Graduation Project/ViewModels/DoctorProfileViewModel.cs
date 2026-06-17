namespace Graduation_Project.ViewModels
{
   
    public class PublicDoctorProfileViewModel
    {
        public int DoctorID { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string Specialization { get; set; } = string.Empty;

        public string Address { get; set; } = string.Empty;

        public string VerificationStatus { get; set; } = string.Empty;
        public DateTime? VerificationDate { get; set; }

   
        public string Bio { get; set; } = string.Empty;

        public int TotalAppointments { get; set; }
        public int CompletedAppointments { get; set; }

        public List<PublicDoctorClinicSummary> Clinics { get; set; } = new();

        
        public int CurrentPatientId { get; set; }

        public bool IsVerified =>
            string.Equals(VerificationStatus, "Verified", StringComparison.OrdinalIgnoreCase);

        public string VerificationBadgeClass => IsVerified ? "badge-verified" : "badge-pending";

        public string VerificationIcon => IsVerified ? "fa-shield-halved" : "fa-clock";

        public string AvatarUrl =>
            $"https://ui-avatars.com/api/?name={Uri.EscapeDataString(FullName)}&background=1baebe&color=fff&size=200&bold=true";
    }

    public class PublicDoctorClinicSummary
    {
        public int ClinicID { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
    }
}
