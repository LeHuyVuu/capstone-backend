namespace capstone_backend.Business.Jobs.VenueSubscription
{
    public interface IVenueSubscriptionWorker
    {
        /// <summary>
        /// Auto expire active venue subscriptions by EndDate and downgrade venue visibility state.
        /// </summary>
        Task AutoExpireVenueSubscriptionsDailyAsync();
    }
}
