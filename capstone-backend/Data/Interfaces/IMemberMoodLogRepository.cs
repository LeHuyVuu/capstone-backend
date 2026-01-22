using capstone_backend.Data.Entities;

namespace capstone_backend.Business.Interfaces;

/// <summary>
/// Member mood log repository interface for member_mood_log specific operations
/// </summary>
public interface IMemberMoodLogRepository : IGenericRepository<MemberMoodLog>
{
    /// <summary>
    /// Get mood logs by member ID
    /// </summary>
    Task<List<MemberMoodLog>> GetByMemberIdAsync(
        int memberId,
        bool includeSoftDeleted = false,
        CancellationToken cancellationToken = default);
}
