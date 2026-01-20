using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace capstone_backend.Data.Entities;

[Index("venue_owner_id", Name = "idx_venue_owner")]
[Index("average_rating", Name = "idx_venue_rating")]
public partial class VenueLocation
{
    [Key]
    public int Id { get; set; }

    public int? LocationTagId { get; set; }

    public int VenueOwnerId { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public string Address { get; set; } = null!;

    public string? Email { get; set; }

    public string? PhoneNumber { get; set; }

    public string? WebsiteUrl { get; set; }

    public DateOnly? OpeningTime { get; set; }

    public DateOnly? ClosingTime { get; set; }

    public bool? IsOpen { get; set; }

    [Precision(18, 2)]
    public decimal? PriceMin { get; set; }

    [Precision(18, 2)]
    public decimal? PriceMax { get; set; }

    [Precision(10, 8)]
    public decimal? Latitude { get; set; }

    [Precision(11, 8)]
    public decimal? Longitude { get; set; }

    [Precision(3, 2)]
    public decimal? AverageRating { get; set; }

    [Precision(18, 2)]
    public decimal? AvarageCost { get; set; }

    public int? ReviewCount { get; set; }

    public string? Status { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public bool? IsDeleted { get; set; }

    [InverseProperty("venue")]
    public virtual ICollection<CheckInHistory> check_in_histories { get; set; } = new List<CheckInHistory>();

    [InverseProperty("venue_location")]
    public virtual ICollection<DatePlanItem> date_plan_items { get; set; } = new List<DatePlanItem>();

    [ForeignKey("location_tag_id")]
    [InverseProperty("venue_locations")]
    public virtual LocationTag? location_tag { get; set; }

    [InverseProperty("venue")]
    public virtual ICollection<Review> reviews { get; set; } = new List<Review>();

    [InverseProperty("venue")]
    public virtual ICollection<VenueLocationAdvertisement> venue_location_advertisements { get; set; } = new List<VenueLocationAdvertisement>();

    [ForeignKey("venue_owner_id")]
    [InverseProperty("venue_locations")]
    public virtual VenueOwnerProfile venue_owner { get; set; } = null!;

    [InverseProperty("venue")]
    public virtual ICollection<venue_subscription_package> venue_subscription_packages { get; set; } = new List<venue_subscription_package>();

    [ForeignKey("venue_id")]
    [InverseProperty("venues")]
    public virtual ICollection<Collection> collections { get; set; } = new List<Collection>();
}
