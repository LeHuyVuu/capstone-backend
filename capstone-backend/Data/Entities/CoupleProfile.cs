using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace capstone_backend.Data.Entities;

[Index("MemberId1", "MemberId2", Name = "idx_couple_members")]
[Index("MemberId1", "MemberId2", Name = "uq_couple_pair", IsUnique = true)]
public partial class CoupleProfile
{
    [Key]
    public int id { get; set; }

    public int MemberId1 { get; set; }

    public int MemberId2 { get; set; }

    public int? CouplePersonalityTypeId { get; set; }

    public int? CoupleMoodTypeId { get; set; }

    public string? CoupleName { get; set; }

    public DateOnly? StartDate { get; set; }

    public DateOnly? AniversaryDate { get; set; }

    [Precision(18, 2)]
    public decimal? BudgetMin { get; set; }

    [Precision(18, 2)]
    public decimal? BudgetMax { get; set; }

    public int? TotalPoints { get; set; }

    public int? InteractionPoints { get; set; }

    public string? Status { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public bool? IsDeleted { get; set; }

    [InverseProperty("Couple")]
    public virtual ICollection<CoupleMoodLog> CoupleMoodLogs { get; set; } = new List<CoupleMoodLog>();

    [ForeignKey("CoupleMoodTypeId")]
    [InverseProperty("CoupleProfiles")]
    public virtual CoupleMoodType? CoupleMoodType { get; set; }

    [ForeignKey("CouplePersonalityTypeId")]
    [InverseProperty("CoupleProfiles")]
    public virtual CouplePersonalityType? CouplePersonalityType { get; set; }

    [InverseProperty("Couple")]
    public virtual ICollection<CoupleProfileChallenge> CoupleProfileChallenges { get; set; } = new List<CoupleProfileChallenge>();

    [InverseProperty("Couple")]
    public virtual ICollection<DatePlan> DatePlans { get; set; } = new List<DatePlan>();

    [InverseProperty("Couple")]
    public virtual ICollection<Leaderboard> Leaderboards { get; set; } = new List<Leaderboard>();

    [ForeignKey("MemberId1")]
    [InverseProperty("CoupleProfilememberId1Navigations")]
    public virtual MemberProfile MemberId1Navigation { get; set; } = null!;

    [ForeignKey("MemberId2")]
    [InverseProperty("CoupleProfilememberId2Navigations")]
    public virtual MemberProfile MemberId2Navigation { get; set; } = null!;
}
