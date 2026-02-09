using capstone_backend.Business.DTOs.Review;

namespace capstone_backend.Business.Interfaces
{
    public interface IReviewService
    {
        Task<int> CheckinAsync(int userId, CheckinRequest request);
    }
}
