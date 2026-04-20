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

        public DateTime SentAtUtc { get; set; } = DateTime.Now;

        public bool IsRead { get; set; }

        public DateTime? ReadAtUtc { get; set; }

        [ForeignKey(nameof(SenderUserId))]
        public virtual ApplicationUser? SenderUser { get; set; }

        [ForeignKey(nameof(ReceiverUserId))]
        public virtual ApplicationUser? ReceiverUser { get; set; }
    }
}
