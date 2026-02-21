using capstone_backend.Business.DTOs.Challenge;
using capstone_backend.Business.DTOs.Common;

namespace capstone_backend.Business.Interfaces
{
    public interface IChallengeService
    {
        Task<object> CreateChallengeAsyncV2(int userId, CreateChallengeRequest request);
        Task<int> DeleteChallengeAsync(int challengeId);
        Task<PagedResult<ChallengeResponse>> GetAllChallengesAsync(int pageNumber, int pageSize);
        Task<ChallengeResponse> UpdateChallengeAsync(int challengeId, UpdateChallengeRequest request);
    }
}
