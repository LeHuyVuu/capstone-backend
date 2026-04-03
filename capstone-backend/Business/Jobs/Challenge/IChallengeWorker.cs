namespace capstone_backend.Business.Jobs.Challenge
{
    public interface IChallengeWorker
    {
        Task RenewDailyCheckinChallengesAsync();
        Task AutoEndChallengeAsync();
        Task AutoInCompleteChallengeAsync();
    }
}
