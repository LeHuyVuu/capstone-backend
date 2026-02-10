namespace capstone_backend.Business.Jobs.Review
{
    public interface IReviewWorker
    {
        Task SendReviewNotificationAsync(int checkInHistoryId);
    }
}
