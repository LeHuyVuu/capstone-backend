using capstone_backend.Business.Interfaces;
using capstone_backend.Data.Entities;

namespace capstone_backend.Data.Interfaces
{
    public interface IMemberAccessoryRepository : IGenericRepository<MemberAccessory>
    {
        Task<IEnumerable<MemberAccessory>> GetOwnerAsync(int memberId, int partnerId, List<int> accessoryIds);
    }
}
