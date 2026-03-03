using capstone_backend.Data.Context;
using capstone_backend.Data.Entities;
using capstone_backend.Data.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace capstone_backend.Data.Repositories
{
    public class CoupleProfileChallengeRepository : GenericRepository<CoupleProfileChallenge>, ICoupleProfileChallengeRepository
    {
        public CoupleProfileChallengeRepository(MyDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<CoupleProfileChallenge>> GetByCoupleIdAndChallengeIdsAsync(int coupleId, List<int> challengeIds)
        {
            return await _dbSet
                .Where(c => c.IsDeleted == false &&
                            c.CoupleId == coupleId && 
                            challengeIds.Contains(c.ChallengeId))
                .ToListAsync();
        }
    }
}
