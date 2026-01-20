using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace capstone_backend.Data.Entities;

public partial class AdsOrder
{
    [Key]
    public int Id { get; set; }

    public int PackageId { get; set; }

    public int AdvertisementId { get; set; }

    [Precision(18, 2)]
    public decimal? PricePaid { get; set; }

    public string? Status { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    [ForeignKey("AdvertisementId")]
    [InverseProperty("AdsOrders")]
    public virtual Advertisement Advertisement { get; set; } = null!;

    [ForeignKey("PackageId")]
    [InverseProperty("AdsOrders")]
    public virtual AdvertisementPackage Package { get; set; } = null!;
}
