namespace capstone_backend.Business.DTOs.CoupleInvitation;

/// <summary>
/// Response DTO cho lời mời ghép đôi
/// </summary>
public class CoupleInvitationResponse
{
    public int InvitationId { get; set; }
    public int SenderMemberId { get; set; }
    public string SenderName { get; set; } = string.Empty;
    public string? SenderAvatarUrl { get; set; }
    public int ReceiverMemberId { get; set; }
    public string ReceiverName { get; set; } = string.Empty;
    public string? ReceiverAvatarUrl { get; set; }
    public string? InviteCodeUsed { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Message { get; set; }
    public DateTime SentAt { get; set; }
    public DateTime? RespondedAt { get; set; }
}
