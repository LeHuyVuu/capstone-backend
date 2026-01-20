using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace capstone_backend.Data.Entities;

public partial class CoupleMoodType
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public bool? IsDeleted { get; set; }

    public bool? IsActive { get; set; }

    [InverseProperty("couple_mood_type")]
    public virtual ICollection<CoupleMoodLog> couple_mood_logs { get; set; } = new List<CoupleMoodLog>();

    [InverseProperty("couple_mood_type")]
    public virtual ICollection<CoupleProfile> couple_profiles { get; set; } = new List<CoupleProfile>();

    [InverseProperty("couple_mood_type")]
    public virtual ICollection<location_tag> location_tags { get; set; } = new List<location_tag>();
}
