using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace capstone_backend.Data.Entities;

public partial class advertisement_package
{
    [Key]
    public int id { get; set; }

    public string name { get; set; } = null!;

    public string? description { get; set; }

    [Precision(18, 2)]
    public decimal price { get; set; }

    public int duration_days { get; set; }

    public int? priority_score { get; set; }

    public string? placement { get; set; }

    public bool? is_active { get; set; }

    public DateTime? created_at { get; set; }

    public DateTime? updated_at { get; set; }

    public bool? is_deleted { get; set; }

    [InverseProperty("package")]
    public virtual ICollection<AdsOrder> ads_orders { get; set; } = new List<AdsOrder>();
}
