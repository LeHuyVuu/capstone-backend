using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace capstone_backend.Data.Entities;

[Index("VenueId", Name = "idx_checkin_venue")]
public partial class CheckInHistory
{
    [Key]
    public int Id { get; set; }

    public int MemberId { get; set; }

    public int VenueId { get; set; }

    [Precision(10, 8)]
    public decimal? Latitude { get; set; }

    [Precision(11, 8)]
    public decimal? Longitude { get; set; }

    public DateTime? CreatedAt { get; set; }

    public bool? IsValid { get; set; }

    [ForeignKey("member_id")]
    [InverseProperty("check_in_histories")]
    public virtual MemberProfile member { get; set; } = null!;

    [ForeignKey("venue_id")]
    [InverseProperty("check_in_histories")]
    public virtual venue_location venue { get; set; } = null!;
}
