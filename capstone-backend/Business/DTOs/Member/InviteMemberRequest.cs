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
    [Required(ErrorMessage = "Invite code is required")]
    [StringLength(10, ErrorMessage = "Invite code must not exceed 10 characters")]
    public string InviteCode { get; set; } = string.Empty;
}
