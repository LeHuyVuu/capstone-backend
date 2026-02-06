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

    [InverseProperty("Member")]
    public virtual ICollection<BlogLike> BlogLikes { get; set; } = new List<BlogLike>();

    [InverseProperty("Member")]
    public virtual ICollection<Blog> Blogs { get; set; } = new List<Blog>();

    [InverseProperty("Member")]
    public virtual ICollection<CheckInHistory> CheckInHistories { get; set; } = new List<CheckInHistory>();

    [InverseProperty("Member")]
    public virtual ICollection<Collection> Collections { get; set; } = new List<Collection>();

    [InverseProperty("Member")]
    public virtual ICollection<CommentLike> CommentLikes { get; set; } = new List<CommentLike>();

    [InverseProperty("Member")]
    public virtual ICollection<Comment> Comments { get; set; } = new List<Comment>();

    [InverseProperty("MemberId1Navigation")]
    public virtual ICollection<CoupleProfile> CoupleProfilememberId1Navigations { get; set; } = new List<CoupleProfile>();

    [InverseProperty("MemberId2Navigation")]
    public virtual ICollection<CoupleProfile> CoupleProfilememberId2Navigations { get; set; } = new List<CoupleProfile>();

    [InverseProperty("Member")]
    public virtual ICollection<MemberAccessory> MemberAccessories { get; set; } = new List<MemberAccessory>();

    [InverseProperty("Member")]
    public virtual ICollection<MemberMoodLog> MemberMoodLogs { get; set; } = new List<MemberMoodLog>();

    [InverseProperty("Member")]
    public virtual ICollection<MemberSubscriptionPackage> MemberSubscriptionPackages { get; set; } = new List<MemberSubscriptionPackage>();

    [ForeignKey("MoodTypesId")]
    [InverseProperty("MemberProfiles")]
    public virtual MoodType? MoodTypes { get; set; }

    [InverseProperty("Member")]
    public virtual ICollection<PersonalityTest> PersonalityTests { get; set; } = new List<PersonalityTest>();

    [InverseProperty("Reporter")]
    public virtual ICollection<Report> Reports { get; set; } = new List<Report>();

    [InverseProperty("Member")]
    public virtual ICollection<ReviewLike> ReviewLikes { get; set; } = new List<ReviewLike>();

    [InverseProperty("Member")]
    public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();

    [InverseProperty("Member")]
    public virtual ICollection<SearchHistory> SearchHistories { get; set; } = new List<SearchHistory>();

    [ForeignKey("UserId")]
    [InverseProperty("MemberProfiles")]
    public virtual UserAccount User { get; set; } = null!;

    [InverseProperty("Member")]
    public virtual ICollection<VoucherItemMember> VoucherItemMembers { get; set; } = new List<VoucherItemMember>();

    [InverseProperty("OrganizerMember")]
    public virtual ICollection<DatePlan> MemberProfiles { get; set; } = new List<DatePlan>();

    [InverseProperty("Member")]
    public virtual ICollection<Interaction> Interactions { get; set; } = new List<Interaction>();

    [InverseProperty("SenderMember")]
    public virtual ICollection<CoupleInvitation> CoupleInvitationsSent { get; set; } = new List<CoupleInvitation>();

    [InverseProperty("ReceiverMember")]
    public virtual ICollection<CoupleInvitation> CoupleInvitationsReceived { get; set; } = new List<CoupleInvitation>();
}
