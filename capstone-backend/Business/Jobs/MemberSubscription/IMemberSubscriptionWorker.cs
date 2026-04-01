namespace capstone_backend.Business.Jobs.MemberSubscription
{
    public interface IMemberSubscriptionWorker
    {
        Task AutoExpireMemberSubscriptionAsync();
    }
}
