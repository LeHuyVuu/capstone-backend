using capstone_backend.Business.DTOs.Challenge;

namespace capstone_backend.Business.Interfaces
{
    public interface IChallengeService
    {
        Task<object> CreateChallengeAsyncV2(int userId, CreateChallengeRequest request);
    }
}
