using capstone_backend.Business.DTOs.Member;

namespace capstone_backend.Business.Interfaces;

public interface IMemberService
{
    /// <summary>
    /// Invite a member to form a couple profile
    /// </summary>
    /// <param name="currentUserId">ID of the user sending the invite</param>
    /// <param name="inviteCode">Invite code of the member to invite</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created couple profile</returns>
    Task<CoupleProfileResponse> InviteMemberAsync(int currentUserId, string inviteCode);

    /// <summary>
    /// Get invite info for the current user
    /// </summary>
    /// <param name="currentUserId">ID of the current user</param>
    /// <returns>Invite code and link</returns>
    Task<InviteInfoResponse> GetInviteInfoAsync(int currentUserId);
}
