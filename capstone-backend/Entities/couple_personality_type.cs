using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace capstone_backend.Entities;

public partial class couple_personality_type
{
    [Key]
    public int id { get; set; }

    public string? name { get; set; }

    public string? description { get; set; }

    public DateTime? created_at { get; set; }

    public DateTime? updated_at { get; set; }

    public bool? is_deleted { get; set; }

    public bool? is_active { get; set; }

    [InverseProperty("couple_personality_type")]
    public virtual ICollection<couple_profile> couple_profiles { get; set; } = new List<couple_profile>();

    [InverseProperty("couple_personality_type")]
    public virtual ICollection<location_tag> location_tags { get; set; } = new List<location_tag>();
}
