using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace capstone_backend.Data.Entities
{
    public partial class Interaction
    {
        [Key]
        public int Id { get; set; }
        public int MemberId { get; set; }
        public int? CoupleId { get; set; }
        public string? InteractionType { get; set; }
        public string? TargetType { get; set; }
        public int? TargetId { get; set; }
        public DateTime CreatedAt { get; set; }

        [ForeignKey("MemberId")]
        [InverseProperty("Interactions")]
        public virtual MemberProfile Member { get; set; } = null!;
    }
}
