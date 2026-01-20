using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace capstone_backend.Data.Entities;

[Index("SeasonKey", "TotalPoints", Name = "idx_leaderboard_points", IsDescending = new[] { false, true })]
public partial class Leaderboard
{
    [Key]
    public int Id { get; set; }

    public int CoupleId { get; set; }

    public string? PeriodType { get; set; }

    public DateTime? PeriodStart { get; set; }

    public DateTime? PeriodEnd { get; set; }

    public string? SeasonKey { get; set; }

    public int? TotalPoints { get; set; }

    public int? RankPosition { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public string? Status { get; set; }

    [ForeignKey("CoupleId")]
    [InverseProperty("Leaderboards")]
    public virtual CoupleProfile Couple { get; set; } = null!;
}
