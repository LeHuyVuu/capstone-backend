using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace capstone_backend.Entities;

[Index("venue_id", Name = "idx_checkin_venue")]
public partial class check_in_history
{
    [Key]
    public int id { get; set; }

    public int member_id { get; set; }

    public int venue_id { get; set; }

    [Precision(10, 8)]
    public decimal? latitude { get; set; }

    [Precision(11, 8)]
    public decimal? longitude { get; set; }

    public DateTime? created_at { get; set; }

    public bool? is_valid { get; set; }

    [ForeignKey("member_id")]
    [InverseProperty("check_in_histories")]
    public virtual member_profile member { get; set; } = null!;

    [ForeignKey("venue_id")]
    [InverseProperty("check_in_histories")]
    public virtual venue_location venue { get; set; } = null!;
}
