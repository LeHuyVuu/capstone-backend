using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace capstone_backend.Data.Entities;

[Index("venue_id", "status", Name = "idx_venue_sub_package")]
public partial class venue_subscription_package
{
    [Key]
    public int id { get; set; }

    public int venue_id { get; set; }

    public int package_id { get; set; }

    public DateTime? start_date { get; set; }

    public DateTime? end_date { get; set; }

    public string? status { get; set; }

    public DateTime? created_at { get; set; }

    public DateTime? updated_at { get; set; }

    [ForeignKey("package_id")]
    [InverseProperty("venue_subscription_packages")]
    public virtual SubscriptionPackage package { get; set; } = null!;

    [ForeignKey("venue_id")]
    [InverseProperty("venue_subscription_packages")]
    public virtual VenueLocation venue { get; set; } = null!;
}
