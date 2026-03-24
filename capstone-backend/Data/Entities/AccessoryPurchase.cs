using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace capstone_backend.Data.Entities
{
    [Index("CoupleId")]
    [Index("AccessoryId")]
    [Index("PurchasedByMemberId")]
    public partial class AccessoryPurchase
    {
        [Key]
        public int Id { get; set; }

        public int CoupleId { get; set; }

        public int AccessoryId { get; set; }

        public int PurchasedByMemberId { get; set; }

        public int PricePoint { get; set; }

        public string Status { get; set; }

        public DateTime? CreatedAt { get; set; }

        [ForeignKey("CoupleId")]
        [InverseProperty("AccessoryPurchases")]
        public virtual CoupleProfile Couple { get; set; } = null!;

        [ForeignKey("AccessoryId")]
        [InverseProperty("AccessoryPurchases")]
        public virtual Accessory Accessory { get; set; } = null!;

        [ForeignKey("PurchasedByMemberId")]
        [InverseProperty("AccessoryPurchases")]
        public virtual MemberProfile PurchasedByMember { get; set; } = null!;

        [InverseProperty("Purchase")]
        public virtual ICollection<MemberAccessory> MemberAccessories { get; set; } = new List<MemberAccessory>();
    }
}
