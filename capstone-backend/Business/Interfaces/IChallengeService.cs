using capstone_backend.Business.DTOs.Challenge;
using capstone_backend.Business.DTOs.Common;

namespace capstone_backend.Business.Interfaces
{
    public interface IChallengeService
    {
        Task<object> CreateChallengeAsyncV2(int userId, CreateChallengeRequest request);
        Task<PagedResult<ChallengeResponse>> GetAllChallengesAsync(int pageNumber, int pageSize);
    }
}
