namespace capstone_backend.Business.Jobs.DatePlan
{
    public interface IDatePlanWorker
    {
        Task StartDatePlanAsync(int datePlanId);
        Task EndDatePlanAsync(int datePlanId);
        Task SendReminderAsync(int datePlanId, string type);
        Task CleanupAllJobsAsync(int datePlanId);
        Task AutoCloseExpiredDatePlanAsync();

        Task SendRejectedNotificationAsync(int datePlanId);
        Task SendCancelledNotificationAsync(int datePlanId);
        Task SendCompletedNotificationAsync(int datePlanId);
    }
}
