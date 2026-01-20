using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace capstone_backend.Data.Entities;

public partial class VoucherItemMember
{
    [Key]
    public int Id { get; set; }

    public int? MemberId { get; set; }

    public int? Quantity { get; set; }

    public int? TotalPointsUsed { get; set; }

    public string? Note { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    [ForeignKey("member_id")]
    [InverseProperty("voucher_item_members")]
    public virtual MemberProfile? member { get; set; }

    [InverseProperty("voucher_item_member")]
    public virtual ICollection<VoucherItem> voucher_items { get; set; } = new List<VoucherItem>();
}
