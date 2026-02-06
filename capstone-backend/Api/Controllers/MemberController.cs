using capstone_backend.Api.Models;
using capstone_backend.Business.DTOs.Member;
using capstone_backend.Business.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace capstone_backend.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class MemberController : BaseController
{
    private readonly IMemberService _memberService;

    public MemberController(IMemberService memberService)
    {
        _memberService = memberService;
    }

    /// <summary>
    /// Invite a member to form a couple profile using their invite code
    /// </summary>
    /// <param name="request">Invite member request containing the invite code</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created couple profile</returns>
    [HttpPost("invite")]
    public async Task<IActionResult> InviteMember(
        [FromBody] InviteMemberRequest request)
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue)
            {
                return UnauthorizedResponse("User ID not found");
            }

            var coupleProfile = await _memberService.InviteMemberAsync(
                currentUserId.Value,
                request.InviteCode);

            return OkResponse(coupleProfile, "Couple profile created successfully");
        }
        catch (InvalidOperationException ex)
        {
            return BadRequestResponse(ex.Message);
        }
    }

    /// <summary>
    /// Get the invite code and sharing link for the current user
    /// </summary>
    /// <returns>Invite code and link</returns>
    [HttpGet("invite-code")]
    public async Task<IActionResult> GetInviteInfo()
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue)
            {
                return UnauthorizedResponse("User ID not found");
            }

            var inviteInfo = await _memberService.GetInviteInfoAsync(currentUserId.Value);
            return OkResponse(inviteInfo, "Invite info retrieved successfully");
        }
        catch (InvalidOperationException ex)
        {
            return BadRequestResponse(ex.Message);
        }
    }
}
