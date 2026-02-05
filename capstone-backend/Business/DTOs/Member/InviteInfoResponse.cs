namespace capstone_backend.Business.DTOs.Member;

/// <summary>
/// Response model for invite info
/// </summary>
public class InviteInfoResponse
{
    public string InviteCode { get; set; } = string.Empty;
    public string InviteLink { get; set; } = string.Empty;
}
