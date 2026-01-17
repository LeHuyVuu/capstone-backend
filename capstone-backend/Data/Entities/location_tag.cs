using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace capstone_backend.Entities;

public partial class location_tag
{
    [Key]
    public int id { get; set; }

    public int? couple_mood_type_id { get; set; }

    public int? couple_personality_type_id { get; set; }

    public DateTime? created_at { get; set; }

    public DateTime? updated_at { get; set; }

    public bool? is_deleted { get; set; }

    [ForeignKey("couple_mood_type_id")]
    [InverseProperty("location_tags")]
    public virtual couple_mood_type? couple_mood_type { get; set; }

    [ForeignKey("couple_personality_type_id")]
    [InverseProperty("location_tags")]
    public virtual couple_personality_type? couple_personality_type { get; set; }

    [InverseProperty("location_tag")]
    public virtual ICollection<venue_location> venue_locations { get; set; } = new List<venue_location>();
}
