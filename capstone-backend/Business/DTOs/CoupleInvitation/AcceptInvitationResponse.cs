namespace capstone_backend.Business.DTOs.CoupleInvitation;

/// <summary>
/// Response DTO khi chấp nhận lời mời (bao gồm thông tin couple profile được tạo)
/// </summary>
public class AcceptInvitationResponse
{
    public int InvitationId { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime RespondedAt { get; set; }
    public CoupleProfileInfo? CoupleProfile { get; set; }
}

public class CoupleProfileInfo
{
    public int CoupleId { get; set; }
    public int MemberId1 { get; set; }
    public string Member1Name { get; set; } = string.Empty;
    public int MemberId2 { get; set; }
    public string Member2Name { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
