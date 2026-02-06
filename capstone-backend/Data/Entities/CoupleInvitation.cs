using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace capstone_backend.Data.Entities;

[Table("couple_invitations")]
[Index("SenderMemberId", Name = "idx_couple_invitations_sender")]
[Index("ReceiverMemberId", Name = "idx_couple_invitations_receiver")]
[Index("Status", Name = "idx_couple_invitations_status")]
public partial class CoupleInvitation
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("sender_member_id")]
    public int SenderMemberId { get; set; }

    [Column("receiver_member_id")]
    public int ReceiverMemberId { get; set; }

    [Column("invite_code_used")]
    public string? InviteCodeUsed { get; set; }

    [Column("status")]
    public string Status { get; set; } = "PENDING";

    [Column("message")]
    public string? Message { get; set; }

    [Column("sent_at")]
    public DateTime SentAt { get; set; }

    [Column("responded_at")]
    public DateTime? RespondedAt { get; set; }

    [Column("created_at")]
    public DateTime? CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }

    [Column("is_deleted")]
    public bool? IsDeleted { get; set; }

    [ForeignKey("SenderMemberId")]
    [InverseProperty("CoupleInvitationsSent")]
    public virtual MemberProfile SenderMember { get; set; } = null!;

    [ForeignKey("ReceiverMemberId")]
    [InverseProperty("CoupleInvitationsReceived")]
    public virtual MemberProfile ReceiverMember { get; set; } = null!;
}
