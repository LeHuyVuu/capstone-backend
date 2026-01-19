using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace capstone_backend.Data.Entities;

public partial class ads_order
{
    [Key]
    public int id { get; set; }

    public int package_id { get; set; }

    public int advertisement_id { get; set; }

    [Precision(18, 2)]
    public decimal? price_paid { get; set; }

    public string? status { get; set; }

    public DateTime? created_at { get; set; }

    public DateTime? updated_at { get; set; }

    [ForeignKey("advertisement_id")]
    [InverseProperty("ads_orders")]
    public virtual advertisement advertisement { get; set; } = null!;

    [ForeignKey("package_id")]
    [InverseProperty("ads_orders")]
    public virtual advertisement_package package { get; set; } = null!;
}
