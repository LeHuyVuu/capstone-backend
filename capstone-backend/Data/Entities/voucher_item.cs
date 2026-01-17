using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace capstone_backend.Entities;

public partial class voucher_item
{
    [Key]
    public int id { get; set; }

    public int voucher_id { get; set; }

    public int? voucher_item_member_id { get; set; }

    public string? status { get; set; }

    public DateTime? acquired_at { get; set; }

    public DateTime? used_at { get; set; }

    public DateTime? created_at { get; set; }

    public DateTime? updated_at { get; set; }

    [ForeignKey("voucher_id")]
    [InverseProperty("voucher_items")]
    public virtual voucher voucher { get; set; } = null!;

    [ForeignKey("voucher_item_member_id")]
    [InverseProperty("voucher_items")]
    public virtual voucher_item_member? voucher_item_member { get; set; }
}
