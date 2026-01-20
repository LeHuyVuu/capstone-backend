using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace capstone_backend.Data.Entities;

[Index("VenueId", "status", Name = "idx_venue_sub_package")]
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

    [ForeignKey("package_id")]
    [InverseProperty("venue_subscription_packages")]
    public virtual SubscriptionPackage package { get; set; } = null!;

    [ForeignKey("venue_id")]
    [InverseProperty("venue_subscription_packages")]
    public virtual VenueLocation venue { get; set; } = null!;
}
