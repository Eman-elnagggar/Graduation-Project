using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Graduation_Project.Models
{
    public class ChatMessage
    {
        [Key]
        public long ChatMessageId { get; set; }

        [Required]
        [StringLength(450)]
        public string SenderUserId { get; set; } = string.Empty;

        [Required]
        [StringLength(450)]
        public string ReceiverUserId { get; set; } = string.Empty;

        [Required]
        [StringLength(2000)]
        public string Message { get; set; } = string.Empty;

        public DateTime SentAtUtc { get; set; } = DateTime.UtcNow;

        public bool IsRead { get; set; }

        public DateTime? ReadAtUtc { get; set; }

        [StringLength(500)]
        public string? AttachmentUrl { get; set; }

        [StringLength(20)]
        public string? AttachmentType { get; set; }

        [StringLength(255)]
        public string? AttachmentName { get; set; }

        [ForeignKey(nameof(SenderUserId))]
        public virtual ApplicationUser? SenderUser { get; set; }

        [ForeignKey(nameof(ReceiverUserId))]
        public virtual ApplicationUser? ReceiverUser { get; set; }
    }
}
