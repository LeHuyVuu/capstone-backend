using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace capstone_backend.Data.Entities;

public partial class VoucherItem
{
    [Key]
    public int Id { get; set; }

    public int VoucherId { get; set; }

    public int? VoucherItemMemberId { get; set; }

    public string? Status { get; set; }

    public DateTime? AcquiredAt { get; set; }

    public DateTime? UsedAt { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    [ForeignKey("VoucherId")]
    [InverseProperty("VoucherItems")]
    public virtual Voucher Voucher { get; set; } = null!;

    [ForeignKey("VoucherItemMemberId")]
    [InverseProperty("VoucherItems")]
    public virtual VoucherItemMember? VoucherItemMember { get; set; }
}
