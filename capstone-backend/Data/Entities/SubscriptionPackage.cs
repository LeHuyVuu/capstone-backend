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

    [InverseProperty("package")]
    public virtual ICollection<MemberSubscriptionPackage> member_subscription_packages { get; set; } = new List<MemberSubscriptionPackage>();

    [InverseProperty("package")]
    public virtual ICollection<venue_subscription_package> venue_subscription_packages { get; set; } = new List<venue_subscription_package>();
}
