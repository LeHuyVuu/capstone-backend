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

    public string[]? DetailTag { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public bool? IsDeleted { get; set; }

    [ForeignKey("CoupleMoodTypeId")]
    [InverseProperty("LocationTags")]
    public virtual CoupleMoodType? CoupleMoodType { get; set; }

    [ForeignKey("CouplePersonalityTypeId")]
    [InverseProperty("LocationTags")]
    public virtual CouplePersonalityType? CouplePersonalityType { get; set; }

    [InverseProperty("LocationTag")]
    public virtual ICollection<VenueLocationTag> VenueLocationTags { get; set; } = new List<VenueLocationTag>();
}
