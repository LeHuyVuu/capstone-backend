using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace capstone_backend.Data.Entities;

[Index("code", Name = "accessories_code_key", IsUnique = true)]
public partial class accessory
{
    [Key]
    public int id { get; set; }

    public string code { get; set; } = null!;

    public string name { get; set; } = null!;

    public string type { get; set; } = null!;

    public string? thumbnail_url { get; set; }

    public string? resource_url { get; set; }

    public int? price_point { get; set; }

    public bool? is_limited { get; set; }

    public int? available_quantity { get; set; }

    public DateTime? available_from { get; set; }

    public DateTime? available_to { get; set; }

    public DateTime? created_at { get; set; }

    public DateTime? updated_at { get; set; }

    public bool? is_deleted { get; set; }

    public string? status { get; set; }

    [InverseProperty("accessory")]
    public virtual ICollection<member_accessory> member_accessories { get; set; } = new List<member_accessory>();
}
