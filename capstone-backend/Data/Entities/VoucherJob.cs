using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace capstone_backend.Data.Entities
{
    public partial class VoucherJob
    {
        [Key]
        public int Id { get; set; }

        public int VoucherId { get; set; }

        public string? JobId { get; set; }

        public string? JobType { get; set; }

        [ForeignKey("VoucherId")]
        [InverseProperty("VoucherJobs")]
        public virtual Voucher Voucher { get; set; } = null!;
    }
}
