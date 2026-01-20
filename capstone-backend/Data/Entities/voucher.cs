using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace capstone_backend.Data.Entities;

public partial class voucher
{
    [Key]
    public int id { get; set; }

    public int? venue_owner_id { get; set; }

    public string code { get; set; } = null!;

    public string? title { get; set; }

    public string? description { get; set; }

    public string? discount_type { get; set; }

    [Precision(18, 2)]
    public decimal? discount_amount { get; set; }

    [Precision(5, 2)]
    public decimal? discount_percent { get; set; }

    public int? quantity { get; set; }

    public int? remaining_quantity { get; set; }

    public int? usage_limit_per_member { get; set; }

    public DateTime? start_date { get; set; }

    public DateTime? end_date { get; set; }

    public string? status { get; set; }

    public DateTime? created_at { get; set; }

    public DateTime? updated_at { get; set; }

    public bool? is_deleted { get; set; }

    [ForeignKey("venue_owner_id")]
    [InverseProperty("vouchers")]
    public virtual VenueOwnerProfile? venue_owner { get; set; }

    [InverseProperty("voucher")]
    public virtual ICollection<voucher_item> voucher_items { get; set; } = new List<voucher_item>();
}
