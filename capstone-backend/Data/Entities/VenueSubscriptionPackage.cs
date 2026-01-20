using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace capstone_backend.Data.Entities;

[Index("VenueId", "Status", Name = "idx_venue_sub_package")]
public partial class VenueSubscriptionPackage
{
    [Key]
    public int Id { get; set; }

    public int VenueId { get; set; }

    public int PackageId { get; set; }

    public DateTime? StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    public string? Status { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    [ForeignKey("PackageId")]
    [InverseProperty("VenueSubscriptionPackages")]
    public virtual SubscriptionPackage Package { get; set; } = null!;

    [ForeignKey("VenueId")]
    [InverseProperty("VenueSubscriptionPackages")]
    public virtual VenueLocation Venue { get; set; } = null!;
}
