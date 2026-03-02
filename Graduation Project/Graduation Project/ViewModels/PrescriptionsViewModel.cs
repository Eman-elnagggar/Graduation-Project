using Graduation_Project.Models;

namespace Graduation_Project.ViewModels
{
    public class PrescriptionsViewModel
    {
        public Patient Patient { get; set; } = null!;
        public string UserName { get; set; } = "Patient";

        // All prescriptions ordered newest-first
        public List<Prescription> Prescriptions { get; set; } = new();

        // Quick-stat counts
        public int TotalCount => Prescriptions.Count;

        // Latest prescription date (null if none)
        public DateTime? LatestDate => Prescriptions.Any()
            ? Prescriptions.Max(p => p.PrescriptionDate)
            : (DateTime?)null;
    }
}
