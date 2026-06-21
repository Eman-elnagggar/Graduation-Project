using System.ComponentModel.DataAnnotations;

namespace Graduation_Project.Models
{
    public class CommunityPost
    {
        public int CommunityPostId { get; set; }

        [Required]
        [MaxLength(150)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [MaxLength(2000)]
        public string Content { get; set; } = string.Empty;

        [MaxLength(60)]
        public string Category { get; set; } = "General";

        [MaxLength(300)]
        public string? ImageUrl { get; set; }

        // Author is either a Patient or a Doctor (exactly one is set).
        public int? PatientID { get; set; }
        public Patient? Patient { get; set; }

        public int? DoctorID { get; set; }
        public Doctor? Doctor { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<CommunityComment> Comments { get; set; } = new List<CommunityComment>();
        public ICollection<CommunityLike> Likes { get; set; } = new List<CommunityLike>();
    }
}
