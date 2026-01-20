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

    [ForeignKey("AdvertisementId")]
    [InverseProperty("VenueLocationAdvertisements")]
    public virtual Advertisement Advertisement { get; set; } = null!;

    [ForeignKey("VenueId")]
    [InverseProperty("VenueLocationAdvertisements")]
    public virtual VenueLocation Venue { get; set; } = null!;
}
