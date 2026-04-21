using Graduation_Project.Models;

namespace Graduation_Project.ViewModels
{
    public class PlacesViewModel
    {
        public Patient Patient { get; set; } = null!;
        public string UserName { get; set; } = "Patient";
        public List<Place> Places { get; set; } = new();
    }
}
