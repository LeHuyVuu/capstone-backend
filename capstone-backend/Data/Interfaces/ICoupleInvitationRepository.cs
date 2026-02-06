using capstone_backend.Data.Entities;

namespace capstone_backend.Data.Interfaces;

public interface ICoupleInvitationRepository
{
    Task<CoupleInvitation?> GetByIdAsync(int id);
    Task<CoupleInvitation?> GetByIdWithMembersAsync(int id);
    Task<List<CoupleInvitation>> GetReceivedInvitationsAsync(int memberId, string? status = null, int page = 1, int pageSize = 20);
    Task<List<CoupleInvitation>> GetSentInvitationsAsync(int memberId, string? status = null, int page = 1, int pageSize = 20);
    Task<bool> HasPendingInvitationBetweenAsync(int memberId1, int memberId2);
    Task<int> CountReceivedInvitationsAsync(int memberId, string? status = null);
    Task<int> CountSentInvitationsAsync(int memberId, string? status = null);
    Task AddAsync(CoupleInvitation invitation);
    Task UpdateAsync(CoupleInvitation invitation);
    Task CancelAllPendingInvitationsForMemberAsync(int memberId);
    Task<int> SaveChangesAsync();
}
