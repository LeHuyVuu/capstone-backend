using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace capstone_backend.Data.Entities;

[Index("season_key", "total_points", Name = "idx_leaderboard_points", IsDescending = new[] { false, true })]
public partial class leaderboard
{
    [Key]
    public int id { get; set; }

    public int couple_id { get; set; }

    public string? period_type { get; set; }

    public DateTime? period_start { get; set; }

    public DateTime? period_end { get; set; }

    public string? season_key { get; set; }

    public int? total_points { get; set; }

    public int? rank_position { get; set; }

    public DateTime? updated_at { get; set; }

    public string? status { get; set; }

    [ForeignKey("couple_id")]
    [InverseProperty("leaderboards")]
    public virtual CoupleProfile couple { get; set; } = null!;
}
