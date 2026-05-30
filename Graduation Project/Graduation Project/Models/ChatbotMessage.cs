using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Graduation_Project.Models
{
    public class ChatbotMessage
    {
        [Key]
        public long ChatbotMessageId { get; set; }

        [Required]
        public int PatientID { get; set; }

        [Required]
        [StringLength(10)]
        public string Role { get; set; } = "User";

        [Required]
        [StringLength(2000)]
        public string Message { get; set; } = string.Empty;

        [StringLength(20)]
        public string? RiskLevel { get; set; }

        [StringLength(2000)]
        public string? Recommendation { get; set; }

        public DateTime SentAtUtc { get; set; } = DateTime.UtcNow;

        [ForeignKey(nameof(PatientID))]
        public Patient? Patient { get; set; }
    }
}
