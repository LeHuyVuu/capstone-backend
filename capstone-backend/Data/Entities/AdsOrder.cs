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

    [ForeignKey("advertisement_id")]
    [InverseProperty("ads_orders")]
    public virtual Advertisement advertisement { get; set; } = null!;

    [ForeignKey("package_id")]
    [InverseProperty("ads_orders")]
    public virtual AdvertisementPackage package { get; set; } = null!;
}
