namespace capstone_backend.Business.DTOs.CoupleInvitation;

/// <summary>
/// Response DTO cho member profile (dùng khi search hoặc get by invite code)
/// </summary>
public class MemberProfileResponse
{
    public int MemberId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public string? Bio { get; set; }
    public string RelationshipStatus { get; set; } = string.Empty;
    public bool CanSendInvitation { get; set; }
}
