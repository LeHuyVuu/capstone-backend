using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace capstone_backend.Data.Entities;

public partial class SubscriptionPackage
{
    [Key]
    public int Id { get; set; }

    public string? PackageName { get; set; }

    [Precision(18, 2)]
    public decimal? Price { get; set; }

    public int? DurationDays { get; set; }

    public string? Type { get; set; }

    public string? Description { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public bool? IsDeleted { get; set; }

    public bool? IsActive { get; set; }

    [InverseProperty("Package")]
    public virtual ICollection<MemberSubscriptionPackage> MemberSubscriptionPackages { get; set; } = new List<MemberSubscriptionPackage>();

    [InverseProperty("Package")]
    public virtual ICollection<VenueSubscriptionPackage> VenueSubscriptionPackages { get; set; } = new List<VenueSubscriptionPackage>();
}
