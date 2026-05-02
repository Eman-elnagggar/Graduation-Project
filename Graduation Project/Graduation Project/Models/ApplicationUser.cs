using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations.Schema;

namespace Graduation_Project.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public DateTime DateOfBirth { get; set; }
        public bool IsActive { get; set; }
        public bool IsBanned { get; set; }
        public string? OriginalEmail { get; set; }
        public DateTime CreatedDate { get; set; }

        [NotMapped]
        public string? Phone
        {
            get => PhoneNumber;
            set => PhoneNumber = value;
        }

        [NotMapped]
        public string UserID
        {
            get => Id;
            set => Id = value;
        }
    }
}
