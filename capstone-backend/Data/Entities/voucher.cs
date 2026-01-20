using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace capstone_backend.Data.Entities;

public partial class Voucher
{
    [Key]
    public int Id { get; set; }

    public int? VenueOwnerId { get; set; }

    public string Code { get; set; } = null!;

    public string? Title { get; set; }

    public string? Description { get; set; }

    public string? DiscountType { get; set; }

    [Precision(18, 2)]
    public decimal? DiscountAmount { get; set; }

    [Precision(5, 2)]
    public decimal? DiscountPercent { get; set; }

    public int? Quantity { get; set; }

    public int? RemainingQuantity { get; set; }

    public int? UsageLimitPerMember { get; set; }

    public DateTime? StartDate { get; set; }

    public DateTime? End_Date { get; set; }

    public string? Status { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public bool? IsDeleted { get; set; }

    [ForeignKey("venue_owner_id")]
    [InverseProperty("vouchers")]
    public virtual VenueOwnerProfile? venue_owner { get; set; }

    [InverseProperty("voucher")]
    public virtual ICollection<voucher_item> voucher_items { get; set; } = new List<voucher_item>();
}
