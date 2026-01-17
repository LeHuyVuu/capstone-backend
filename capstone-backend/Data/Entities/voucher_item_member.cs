using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace capstone_backend.Entities;

public partial class voucher_item_member
{
    [Key]
    public int id { get; set; }

    public int? member_id { get; set; }

    public int? quantity { get; set; }

    public int? total_points_used { get; set; }

    public string? note { get; set; }

    public DateTime? created_at { get; set; }

    public DateTime? updated_at { get; set; }

    [ForeignKey("member_id")]
    [InverseProperty("voucher_item_members")]
    public virtual member_profile? member { get; set; }

    [InverseProperty("voucher_item_member")]
    public virtual ICollection<voucher_item> voucher_items { get; set; } = new List<voucher_item>();
}
