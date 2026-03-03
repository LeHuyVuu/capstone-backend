using capstone_backend.Business.DTOs.Challenge;
using capstone_backend.Business.DTOs.Common;

namespace capstone_backend.Business.Interfaces
{
    public interface IChallengeService
    {
        Task<int> ChangeChallengeStatusAsync(int challengeId, string newStatus);
        Task<ChallengeResponse> CreateChallengeAsyncV2(int userId, CreateChallengeRequest request);
        Task<int> DeleteChallengeAsync(int challengeId);
        Task<PagedResult<ChallengeResponse>> GetAllChallengesAsync(int pageNumber, int pageSize);
        Task<ChallengeResponse> GetChallengeByIdAsync(int challengeId);
        Task<MemberChallengeDetailResponse> GetMemberChallengeByIdAsync(int userId, int challengeId);
        Task<PagedResult<MemberChallengeResponse>> GetMemberChallengesAsync(int userId, int pageNumber, int pageSize);
        Task<PagedResult<CoupleChallengeListItemResponse>> GetMyCoupleChallengesAsync(int userId, CoupleChallengeQuery query);
        Task<ChallengeResponse> UpdateChallengeAsync(int challengeId, UpdateChallengeRequest request);
    }
}
