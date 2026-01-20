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

    [InverseProperty("Advertisement")]
    public virtual ICollection<AdsOrder> AdsOrders { get; set; } = new List<AdsOrder>();

    [InverseProperty("Advertisement")]
    public virtual ICollection<VenueLocationAdvertisement> VenueLocationAdvertisements { get; set; } = new List<VenueLocationAdvertisement>();

    [ForeignKey("VenueOwnerId")]
    [InverseProperty("Advertisements")]
    public virtual VenueOwnerProfile VenueOwner { get; set; } = null!;
}
