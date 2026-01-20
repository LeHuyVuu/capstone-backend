using capstone_backend.Data.Entities;

namespace capstone_backend.Business.Interfaces;

/// <summary>
/// Member profile repository interface for member_profile specific operations
/// </summary>
public interface IMemberProfileRepository : IRepository<MemberProfile>
{
    /// <summary>
    /// Get member profile by invite code
    /// </summary>
    Task<MemberProfile?> GetByInviteCodeAsync(
        string inviteCode,
        bool includeSoftDeleted = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get member profile by user ID
    /// </summary>
    Task<MemberProfile?> GetByUserIdAsync(
        int userId,
        bool includeSoftDeleted = false,
        CancellationToken cancellationToken = default);
}
