using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace capstone_backend.Data.Entities;

[Index("UserId", Name = "idx_member_user_id")]
[Index("InviteCode", Name = "member_profiles_invite_code_key", IsUnique = true)]
public partial class MemberProfile
{
    [Key]
    public int Id { get; set; }

    public int UserId { get; set; }

    public int? MoodTypesId { get; set; }

    public string? FullName { get; set; }

    public DateOnly? DateOfBirth { get; set; }

    public string? Gender { get; set; }

    public string? Bio { get; set; }

    public string? RelationshipStatus { get; set; }

    [Precision(10, 8)]
    public decimal? HomeLatitude { get; set; }

    [Precision(11, 8)]
    public decimal? HomeLongitude { get; set; }

    [Precision(18, 2)]
    public decimal? BudgetMin { get; set; }

    [Precision(18, 2)]
    public decimal? BudgetMax { get; set; }

    [Column(TypeName = "jsonb")]
    public string? Interests { get; set; }

    [Column(TypeName = "jsonb")]
    public string? AvailableTime { get; set; }

    [StringLength(10)]
    public string? InviteCode { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public bool? IsDeleted { get; set; }

    [InverseProperty("member")]
    public virtual ICollection<BlogLike> blog_likes { get; set; } = new List<BlogLike>();

    [InverseProperty("member")]
    public virtual ICollection<Blog> blogs { get; set; } = new List<Blog>();

    [InverseProperty("member")]
    public virtual ICollection<CheckInHistory> check_in_histories { get; set; } = new List<CheckInHistory>();

    [InverseProperty("member")]
    public virtual ICollection<Collection> collections { get; set; } = new List<Collection>();

    [InverseProperty("member")]
    public virtual ICollection<CommentLike> comment_likes { get; set; } = new List<CommentLike>();

    [InverseProperty("member")]
    public virtual ICollection<Comment> comments { get; set; } = new List<Comment>();

    [InverseProperty("member_id_1Navigation")]
    public virtual ICollection<CoupleProfile> couple_profilemember_id_1Navigations { get; set; } = new List<CoupleProfile>();

    [InverseProperty("member_id_2Navigation")]
    public virtual ICollection<CoupleProfile> couple_profilemember_id_2Navigations { get; set; } = new List<CoupleProfile>();

    [InverseProperty("member")]
    public virtual ICollection<MemberAccessory> member_accessories { get; set; } = new List<MemberAccessory>();

    [InverseProperty("member")]
    public virtual ICollection<MemberMoodLog> member_mood_logs { get; set; } = new List<MemberMoodLog>();

    [InverseProperty("member")]
    public virtual ICollection<MemberSubscriptionPackage> member_subscription_packages { get; set; } = new List<MemberSubscriptionPackage>();

    [ForeignKey("mood_types_id")]
    [InverseProperty("member_profiles")]
    public virtual MoodType? mood_types { get; set; }

    [InverseProperty("member")]
    public virtual ICollection<PersonalityTest> personality_tests { get; set; } = new List<PersonalityTest>();

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
