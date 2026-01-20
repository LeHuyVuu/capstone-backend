using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace capstone_backend.Data.Entities;

public partial class CouplePersonalityType
{
    [Key]
    public int Id { get; set; }

    public string? Name { get; set; }

    public string? Description { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public bool? IsDeleted { get; set; }

    public bool? IsActive { get; set; }

    [InverseProperty("CouplePersonalityType")]
    public virtual ICollection<CoupleProfile> CoupleProfiles { get; set; } = new List<CoupleProfile>();

    [InverseProperty("CouplePersonalityType")]
    public virtual ICollection<LocationTag> LocationTags { get; set; } = new List<LocationTag>();
}
