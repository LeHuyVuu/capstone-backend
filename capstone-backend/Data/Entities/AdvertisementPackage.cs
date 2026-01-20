using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace capstone_backend.Data.Entities;

public partial class AdvertisementPackage
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    [Precision(18, 2)]
    public decimal Price { get; set; }

    public int DurationDays { get; set; }

    public int? PriorityScore { get; set; }

    public string? Placement { get; set; }

    public bool? IsActive { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public bool? IsDeleted { get; set; }

    [InverseProperty("package")]
    public virtual ICollection<AdsOrder> ads_orders { get; set; } = new List<AdsOrder>();
}
