using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace capstone_backend.Data.Entities;

[Index("member_id_1", "member_id_2", Name = "idx_couple_members")]
[Index("member_id_1", "member_id_2", Name = "uq_couple_pair", IsUnique = true)]
public partial class couple_profile
{
    [Key]
    public int id { get; set; }

    public int member_id_1 { get; set; }

    public int member_id_2 { get; set; }

    public int? couple_personality_type_id { get; set; }

    public int? couple_mood_type_id { get; set; }

    public string? couple_name { get; set; }

    public DateOnly? start_date { get; set; }

    public DateOnly? aniversary_date { get; set; }

    [Precision(18, 2)]
    public decimal? budget_min { get; set; }

    [Precision(18, 2)]
    public decimal? budget_max { get; set; }

    public int? total_points { get; set; }

    public int? interaction_points { get; set; }

    public string? status { get; set; }

    public DateTime? created_at { get; set; }

    public DateTime? updated_at { get; set; }

    public bool? is_deleted { get; set; }

    [InverseProperty("couple")]
    public virtual ICollection<couple_mood_log> couple_mood_logs { get; set; } = new List<couple_mood_log>();

    [ForeignKey("couple_mood_type_id")]
    [InverseProperty("couple_profiles")]
    public virtual couple_mood_type? couple_mood_type { get; set; }

    [ForeignKey("couple_personality_type_id")]
    [InverseProperty("couple_profiles")]
    public virtual couple_personality_type? couple_personality_type { get; set; }

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
