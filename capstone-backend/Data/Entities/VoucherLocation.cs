using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace capstone_backend.Data.Entities
{
    public partial class VoucherLocation
    {
        [Key]
        public int Id { get; set; }
        
        public int VoucherId { get; set; }

        public int VenueLocationId { get; set; }

        public DateTime CreatedAt { get; set; }

        [ForeignKey("VoucherId")]
        [InverseProperty("VoucherLocations")]
        public virtual Voucher Voucher { get; set; } = null!;

        [ForeignKey("VenueLocationId")]
        [InverseProperty("VoucherLocations")]
        public virtual VenueLocation VenueLocation { get; set; } = null!;
    }
}
