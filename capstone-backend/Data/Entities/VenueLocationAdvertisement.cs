using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace capstone_backend.Data.Entities;

public partial class VenueLocationAdvertisement
{
    [Key]
    public int Id { get; set; }

    public int AdvertisementId { get; set; }

    public int VenueId { get; set; }

    public int? PriorityScore { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public string? Status { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    [ForeignKey("advertisement_id")]
    [InverseProperty("venue_location_advertisements")]
    public virtual Advertisement advertisement { get; set; } = null!;

    [ForeignKey("venue_id")]
    [InverseProperty("venue_location_advertisements")]
    public virtual VenueLocation venue { get; set; } = null!;
}
