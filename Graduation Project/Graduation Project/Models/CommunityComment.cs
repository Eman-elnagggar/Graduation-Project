using System.ComponentModel.DataAnnotations;

namespace Graduation_Project.Models
{
    public class CommunityComment
    {
        public int CommunityCommentId { get; set; }

        public int CommunityPostId { get; set; }
        public CommunityPost? Post { get; set; }

        public int PatientID { get; set; }
        public Patient? Patient { get; set; }

        [Required]
        [MaxLength(1000)]
        public string Content { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
