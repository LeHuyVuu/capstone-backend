using capstone_backend.Business.Interfaces;
using capstone_backend.Data.Entities;

namespace capstone_backend.Data.Interfaces
{
    public interface ICoupleProfileChallengeRepository : IGenericRepository<CoupleProfileChallenge>
    {
        Task<IEnumerable<CoupleProfileChallenge>> GetByCoupleIdAndChallengeIdsAsync(int coupleId, List<int> challengeIds);
    }
}
