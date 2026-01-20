using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace capstone_backend.Data.Entities;

public partial class LocationTag
{
    [Key]
    public int Id { get; set; }

    public int? CoupleMoodTypeId { get; set; }

    public int? CouplePersonalityTypeId { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public bool? IsDeleted { get; set; }

    [ForeignKey("couple_mood_type_id")]
    [InverseProperty("location_tags")]
    public virtual CoupleMoodType? couple_mood_type { get; set; }

    [ForeignKey("couple_personality_type_id")]
    [InverseProperty("location_tags")]
    public virtual CouplePersonalityType? couple_personality_type { get; set; }

    [InverseProperty("location_tag")]
    public virtual ICollection<VenueLocation> venue_locations { get; set; } = new List<VenueLocation>();
}
