using capstone_backend.Business.Interfaces;
using capstone_backend.Data.Entities;

namespace capstone_backend.Data.Interfaces
{
    public interface IMemberAccessoryRepository : IGenericRepository<MemberAccessory>
    {
        Task<IEnumerable<MemberAccessory>> GetEquippedByMemberIdAndTypeAsync(int memberId, string type, int id);
        Task<IEnumerable<MemberAccessory>> GetOwnerAsync(int memberId, int partnerId, List<int> accessoryIds);
        Task<IEnumerable<MemberAccessory>> GetEquippedByMemberIdAsync(int memberId);
        Task<bool> HasRewarded(List<int> memberIds, int kingId, int queenId, DateTime periodStart, DateTime periodEnd);
    }
}
