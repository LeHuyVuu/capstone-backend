using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace capstone_backend.Data.Entities;

public partial class Advertisement
{
    [Key]
    public int Id { get; set; }

    public int VenueOwnerId { get; set; }

    public string? Title { get; set; }

    public string? Content { get; set; }

    public string? BannerUrl { get; set; }

    public string? TargetUrl { get; set; }

    public string? PlacementType { get; set; }

    public string? RejectionReason { get; set; }

    public string? Status { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public bool? IsDeleted { get; set; }

    [InverseProperty("advertisement")]
    public virtual ICollection<AdsOrder> ads_orders { get; set; } = new List<AdsOrder>();

    [InverseProperty("advertisement")]
    public virtual ICollection<venue_location_advertisement> venue_location_advertisements { get; set; } = new List<venue_location_advertisement>();

    [ForeignKey("venue_owner_id")]
    [InverseProperty("advertisements")]
    public virtual venue_owner_profile venue_owner { get; set; } = null!;
}
