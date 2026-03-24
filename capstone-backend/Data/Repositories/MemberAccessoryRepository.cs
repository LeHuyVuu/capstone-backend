using capstone_backend.Data.Context;
using capstone_backend.Data.Entities;
using capstone_backend.Data.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace capstone_backend.Data.Repositories
{
    public class MemberAccessoryRepository : GenericRepository<MemberAccessory>, IMemberAccessoryRepository
    {
        public MemberAccessoryRepository(MyDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<MemberAccessory>> GetOwnerAsync(int memberId, int partnerId, List<int> accessoryIds)
        {
            return await _dbSet
                .Where(ma => accessoryIds.Contains(ma.AccessoryId.Value) && (ma.MemberId == memberId || ma.MemberId == partnerId))
                .ToListAsync();
        }
    }
}
