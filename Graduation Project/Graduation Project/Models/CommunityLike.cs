namespace Graduation_Project.Models
{
    public class CommunityLike
    {
        public int CommunityLikeId { get; set; }

        public int CommunityPostId { get; set; }
        public CommunityPost? Post { get; set; }

        public int PatientID { get; set; }
        public Patient? Patient { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
