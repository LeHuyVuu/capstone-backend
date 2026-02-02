using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace capstone_backend.Data.Entities;

/// <summary>
/// Junction table for many-to-many relationship between VenueLocation and LocationTag
/// </summary>
public partial class VenueLocationTag
{
    [Key]
    public int Id { get; set; }

    public int VenueLocationId { get; set; }

    public int LocationTagId { get; set; }

    public DateTime? CreatedAt { get; set; }

    public bool? IsDeleted { get; set; }

    [ForeignKey("VenueLocationId")]
    [InverseProperty("VenueLocationTags")]
    public virtual VenueLocation VenueLocation { get; set; } = null!;

    [ForeignKey("LocationTagId")]
    [InverseProperty("VenueLocationTags")]
    public virtual LocationTag LocationTag { get; set; } = null!;
}
