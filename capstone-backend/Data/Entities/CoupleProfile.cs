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

    [InverseProperty("couple")]
    public virtual ICollection<CoupleMoodLog> couple_mood_logs { get; set; } = new List<CoupleMoodLog>();

    [ForeignKey("couple_mood_type_id")]
    [InverseProperty("couple_profiles")]
    public virtual CoupleMoodType? couple_mood_type { get; set; }

    [ForeignKey("couple_personality_type_id")]
    [InverseProperty("couple_profiles")]
    public virtual CouplePersonalityType? couple_personality_type { get; set; }

    [InverseProperty("couple")]
    public virtual ICollection<couple_profile_challenge> couple_profile_challenges { get; set; } = new List<couple_profile_challenge>();

    [InverseProperty("couple")]
    public virtual ICollection<date_plan> date_plans { get; set; } = new List<date_plan>();

    [InverseProperty("couple")]
    public virtual ICollection<leaderboard> leaderboards { get; set; } = new List<leaderboard>();

    [ForeignKey("member_id_1")]
    [InverseProperty("couple_profilemember_id_1Navigations")]
    public virtual member_profile member_id_1Navigation { get; set; } = null!;

    [ForeignKey("member_id_2")]
    [InverseProperty("couple_profilemember_id_2Navigations")]
    public virtual member_profile member_id_2Navigation { get; set; } = null!;
}
