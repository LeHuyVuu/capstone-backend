namespace capstone_backend.Business.Jobs.Notification
{
    public interface INotificationWorker
    {
        Task SendPushNotificationAsync(int notificationId);
    }
}
