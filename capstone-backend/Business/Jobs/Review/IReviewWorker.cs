namespace capstone_backend.Business.Jobs.Review
{
    public interface IReviewWorker
    {
        Task SendReviewNotificationAsync(int checkInHistoryId);
        Task EvaluateReviewRelevanceAsync(int reviewId);
        Task RecountReviewAsync(int venueId);
    }
}
