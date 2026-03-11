using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace capstone_backend.Data.Entities;


[Table("venue_location_category")]
public partial class VenueLocationCategory
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Required]
    [Column("venue_location_id")]
    public int VenueLocationId { get; set; }


    [Required]
    [Column("category_id")]
    public int CategoryId { get; set; }


    [Column("is_primary")]
    public bool IsPrimary { get; set; } = false;


    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;


    [Column("is_deleted")]
    public bool IsDeleted { get; set; } = false;

    [ForeignKey("VenueLocationId")]
    [InverseProperty("VenueLocationCategories")]
    public virtual VenueLocation VenueLocation { get; set; } = null!;


    [ForeignKey("CategoryId")]
    [InverseProperty("VenueLocationCategories")]
    public virtual Category Category { get; set; } = null!;
}
