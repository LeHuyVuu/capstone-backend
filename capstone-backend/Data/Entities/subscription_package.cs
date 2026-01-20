using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace capstone_backend.Data.Entities;

public partial class subscription_package
{
    [Key]
    public int id { get; set; }

    public string? package_name { get; set; }

    [Precision(18, 2)]
    public decimal? price { get; set; }

    public int? duration_days { get; set; }

    public string? type { get; set; }

    public string? description { get; set; }

    public DateTime? created_at { get; set; }

    public DateTime? updated_at { get; set; }

    public bool? is_deleted { get; set; }

    public bool? is_active { get; set; }

    [InverseProperty("package")]
    public virtual ICollection<MemberSubscriptionPackage> member_subscription_packages { get; set; } = new List<MemberSubscriptionPackage>();

    [InverseProperty("package")]
    public virtual ICollection<venue_subscription_package> venue_subscription_packages { get; set; } = new List<venue_subscription_package>();
}
