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

    [ForeignKey("MemberId")]
    [InverseProperty("VoucherItemMembers")]
    public virtual MemberProfile? Member { get; set; }

    [InverseProperty("VoucherItemMember")]
    public virtual ICollection<VoucherItem> VoucherItems { get; set; } = new List<VoucherItem>();
}
