using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace capstone_backend.Data.Entities
{
    public partial class VoucherItemJob
    {
        [Key]
        public int Id { get; set; }
        public int VoucherItemId { get; set; }
        public string? JobId { get; set; }
        public string? JobType { get; set; }

        [ForeignKey("VoucherItemId")]
        [InverseProperty("VoucherItemJobs")]
        public virtual VoucherItem VoucherItem { get; set; } = null!;
    }
}
