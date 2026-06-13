using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Graduation_Project.Models
{
    public class UserPushSubscription
    {
        [Key]
        public int Id { get; set; }

        [Required, StringLength(450)]
        public string UserId { get; set; } = null!;

        [Required, StringLength(1000)]
        public string Endpoint { get; set; } = null!;

        [Required, StringLength(500)]
        public string P256DH { get; set; } = null!;

        [Required, StringLength(200)]
        public string Auth { get; set; } = null!;

        public DateTime CreatedAt { get; set; }

        [ForeignKey(nameof(UserId))]
        public virtual ApplicationUser? User { get; set; }
    }
}
