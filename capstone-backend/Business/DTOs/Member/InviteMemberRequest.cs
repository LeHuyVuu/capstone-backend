using System.ComponentModel.DataAnnotations;

namespace capstone_backend.Business.DTOs.Member;

/// <summary>
/// Request model for inviting a member to form a couple
/// </summary>
public class InviteMemberRequest
{
    /// <summary>
    /// Invite code of the member to invite
    /// </summary>
    [Required(ErrorMessage = "Mã mời là bắt buộc")]
    [StringLength(10, ErrorMessage = "Mã mời không được vượt quá 10 ký tự")]
    public string InviteCode { get; set; } = string.Empty;
}
