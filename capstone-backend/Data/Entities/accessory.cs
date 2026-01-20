using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace capstone_backend.Data.Entities;

[Index("Code", Name = "accessories_code_key", IsUnique = true)]
public partial class Accessory
{
    [Key]
    public int Id { get; set; }

    public string Code { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string Type { get; set; } = null!;

    public string? ThumbnailUrl { get; set; }

    public string? ResourceUrl { get; set; }

    public int? PricePoint { get; set; }

    public bool? IsLimited { get; set; }

    public int? AvailableQuantity { get; set; }

    public DateTime? AvailableFrom { get; set; }

    public DateTime? AvailableTo { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public bool? IsDeleted { get; set; }

    public string? Status { get; set; }

    [InverseProperty("Accessory")]
    public virtual ICollection<MemberAccessory> MemberAccessories { get; set; } = new List<MemberAccessory>();
}
