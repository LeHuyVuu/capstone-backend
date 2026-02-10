using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace capstone_backend.Data.Entities
{
    [Index(nameof(ReviewId), IsUnique = true)]
    public partial class ReviewReply
    {
        [Key]
        public int Id { get; set; }
        public int ReviewId { get; set; }
        public int UserId { get; set; }
        public string Content { get; set; }

        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        [ForeignKey("ReviewId")]
        [InverseProperty("ReviewReply")]
        public virtual Review Review { get; set; } = null!;

        [ForeignKey("UserId")]
        [InverseProperty("ReviewReplies")]
        public virtual UserAccount User { get; set; } = null!;
    }
}
