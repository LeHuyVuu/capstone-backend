using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace capstone_backend.Data.Entities;

[Index("VenueOwnerId", Name = "idx_venue_owner")]
[Index("AverageRating", Name = "idx_venue_rating")]
[Index("Latitude", "Longitude", Name = "idx_venue_location")]
public partial class VenueLocation
{
    [Key]
    public int Id { get; set; }

    public int VenueOwnerId { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public string Address { get; set; } = null!;

    public string? Email { get; set; }

    public string? PhoneNumber { get; set; }

    public string? WebsiteUrl { get; set; }

    [Precision(18, 2)]
    public decimal? PriceMin { get; set; }

    [Precision(18, 2)]
    public decimal? PriceMax { get; set; }

    [Precision(10, 8)]
    public decimal? Latitude { get; set; }

    [Precision(11, 8)]
    public decimal? Longitude { get; set; }

    public string? Area { get; set; }

    [Precision(3, 2)]
    public decimal? AverageRating { get; set; }

    [Precision(18, 2)]
    public decimal? AvarageCost { get; set; }

    public int? ReviewCount { get; set; }
    public int? FavoriteCount { get; set; }

    public string? Status { get; set; }

    public string? CoverImage { get; set; }

    public string? InteriorImage { get; set; }

    public string? Category { get; set; }

    public string? FullPageMenuImage { get; set; }

    public bool? IsOwnerVerified { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public bool? IsDeleted { get; set; }

    [InverseProperty("Venue")]
    public virtual ICollection<CheckInHistory> CheckInHistories { get; set; } = new List<CheckInHistory>();

    [InverseProperty("VenueLocation")]
    public virtual ICollection<DatePlanItem> DatePlanItems { get; set; } = new List<DatePlanItem>();

    [InverseProperty("Venue")]
    public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();

    [InverseProperty("Venue")]
    public virtual ICollection<VenueLocationAdvertisement> VenueLocationAdvertisements { get; set; } = new List<VenueLocationAdvertisement>();

    [ForeignKey("VenueOwnerId")]
    [InverseProperty("VenueLocations")]
    public virtual VenueOwnerProfile VenueOwner { get; set; } = null!;

    [InverseProperty("Venue")]
    public virtual ICollection<VenueSubscriptionPackage> VenueSubscriptionPackages { get; set; } = new List<VenueSubscriptionPackage>();

    [InverseProperty("Venues")]
    public virtual ICollection<Collection> Collections { get; set; } = new List<Collection>();

    [InverseProperty("VenueLocation")]
    public virtual ICollection<VenueOpeningHour> VenueOpeningHours { get; set; } = new List<VenueOpeningHour>();

    [InverseProperty("VenueLocation")]
    public virtual ICollection<VenueLocationTag> VenueLocationTags { get; set; } = new List<VenueLocationTag>();
}
