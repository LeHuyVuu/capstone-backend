using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace capstone_backend.Data.Entities;

[Index("user_id", Name = "idx_member_user_id")]
[Index("invite_code", Name = "member_profiles_invite_code_key", IsUnique = true)]
public partial class member_profile
{
    [Key]
    public int id { get; set; }

    public int user_id { get; set; }

    public int? mood_types_id { get; set; }

    public string? full_name { get; set; }

    public DateOnly? date_of_birth { get; set; }

    public string? gender { get; set; }

    public string? bio { get; set; }

    public string? relationship_status { get; set; }

    [Precision(10, 8)]
    public decimal? home_latitude { get; set; }

    [Precision(11, 8)]
    public decimal? home_longitude { get; set; }

    [Precision(18, 2)]
    public decimal? budget_min { get; set; }

    [Precision(18, 2)]
    public decimal? budget_max { get; set; }

    [Column(TypeName = "jsonb")]
    public string? interests { get; set; }

    [Column(TypeName = "jsonb")]
    public string? available_time { get; set; }

    [StringLength(10)]
    public string? invite_code { get; set; }

    public DateTime? created_at { get; set; }

    public DateTime? updated_at { get; set; }

    public bool? is_deleted { get; set; }

    [InverseProperty("member")]
    public virtual ICollection<blog_like> blog_likes { get; set; } = new List<blog_like>();

    [InverseProperty("member")]
    public virtual ICollection<blog> blogs { get; set; } = new List<blog>();

    [InverseProperty("member")]
    public virtual ICollection<check_in_history> check_in_histories { get; set; } = new List<check_in_history>();

    [InverseProperty("member")]
    public virtual ICollection<collection> collections { get; set; } = new List<collection>();

    [InverseProperty("member")]
    public virtual ICollection<comment_like> comment_likes { get; set; } = new List<comment_like>();

    [InverseProperty("member")]
    public virtual ICollection<comment> comments { get; set; } = new List<comment>();

    [InverseProperty("member_id_1Navigation")]
    public virtual ICollection<couple_profile> couple_profilemember_id_1Navigations { get; set; } = new List<couple_profile>();

    [InverseProperty("member_id_2Navigation")]
    public virtual ICollection<couple_profile> couple_profilemember_id_2Navigations { get; set; } = new List<couple_profile>();

    [InverseProperty("member")]
    public virtual ICollection<member_accessory> member_accessories { get; set; } = new List<member_accessory>();

    [InverseProperty("member")]
    public virtual ICollection<member_mood_log> member_mood_logs { get; set; } = new List<member_mood_log>();

    [InverseProperty("member")]
    public virtual ICollection<member_subscription_package> member_subscription_packages { get; set; } = new List<member_subscription_package>();

    [ForeignKey("mood_types_id")]
    [InverseProperty("member_profiles")]
    public virtual mood_type? mood_types { get; set; }

    [InverseProperty("member")]
    public virtual ICollection<personality_test> personality_tests { get; set; } = new List<personality_test>();

    [InverseProperty("reporter")]
    public virtual ICollection<report> reports { get; set; } = new List<report>();

    [InverseProperty("member")]
    public virtual ICollection<review_like> review_likes { get; set; } = new List<review_like>();

    [InverseProperty("member")]
    public virtual ICollection<review> reviews { get; set; } = new List<review>();

    [InverseProperty("member")]
    public virtual ICollection<search_history> search_histories { get; set; } = new List<search_history>();

    [ForeignKey("user_id")]
    [InverseProperty("member_profiles")]
    public virtual UserAccount user { get; set; } = null!;

    [InverseProperty("member")]
    public virtual ICollection<voucher_item_member> voucher_item_members { get; set; } = new List<voucher_item_member>();
}
