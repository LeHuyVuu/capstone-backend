using capstone_backend.Business.DTOs.Review;

namespace capstone_backend.Business.Interfaces
{
    public interface IReviewService
    {
        Task<int> CheckinAsync(int userId, CheckinRequest request);
        Task<int> DeleteReviewAsync(int userId, int reviewId);
        Task<int> DeleteReviewReplyAsync(int userId, int reviewId);
        Task<int> ReplyToReviewAsync(int userId, int reviewId, ReviewReplyRequest request);
        Task<int> SubmitReviewAsync(int userId, CreateReviewRequest request);
        Task<int> UpdateReplyReviewAsync(int userId, int reviewId, ReviewReplyRequest request);
        Task<int> UpdateReviewAsync(int userId, int reviewId, UpdateReviewRequest request);
        Task<int> ValidateCheckinAsync(int userId, int checkInId, CheckinRequest request);
    }
}
