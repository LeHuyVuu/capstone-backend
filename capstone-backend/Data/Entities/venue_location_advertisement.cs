using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace capstone_backend.Data.Entities;

public partial class venue_location_advertisement
{
    [Key]
    public int id { get; set; }

    public int advertisement_id { get; set; }

    public int venue_id { get; set; }

    public int? priority_score { get; set; }

    public DateTime start_date { get; set; }

    public DateTime end_date { get; set; }

    public string? status { get; set; }

    public DateTime? created_at { get; set; }

    public DateTime? updated_at { get; set; }

    [ForeignKey("advertisement_id")]
    [InverseProperty("venue_location_advertisements")]
    public virtual Advertisement advertisement { get; set; } = null!;

    [ForeignKey("venue_id")]
    [InverseProperty("venue_location_advertisements")]
    public virtual VenueLocation venue { get; set; } = null!;
}
