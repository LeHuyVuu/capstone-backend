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
        [FromBody] InviteMemberRequest request,
        CancellationToken cancellationToken = default)
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
                request.InviteCode,
                cancellationToken);

            return OkResponse(coupleProfile, "Couple profile created successfully");
        }
        catch (InvalidOperationException ex)
        {
            return BadRequestResponse(ex.Message);
        }
    }
}
