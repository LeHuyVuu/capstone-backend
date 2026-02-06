using capstone_backend.Data.Entities;
using capstone_backend.Business.Interfaces;

namespace capstone_backend.Data.Interfaces;

public interface ICoupleProfileRepository : IGenericRepository<CoupleProfile>
{
    /// <summary>
    /// Get couple profile by member ID
    /// </summary>
    Task<CoupleProfile?> GetByMemberIdAsync(
        int memberId,
        bool includeSoftDeleted = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get couple profile by both member IDs (order doesn't matter)
    /// </summary>
    Task<CoupleProfile?> GetByBothMemberIdsAsync(
        int memberId1,
        int memberId2,
        bool includeSoftDeleted = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all active couple profiles
    /// </summary>
    Task<List<CoupleProfile>> GetAllActiveAsync(
        bool includeSoftDeleted = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get active couple profile by member ID
    /// </summary>
    Task<CoupleProfile?> GetActiveCoupleByMemberIdAsync(
        int memberId,
        CancellationToken cancellationToken = default);
    Task<(int userId1, int userId2)> GetCoupleUserIdsAsync(int coupleId);
}
