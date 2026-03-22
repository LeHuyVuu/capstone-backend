namespace capstone_backend.Business.Jobs.Leaderboard
{
    public interface ILeaderboardWorker
    {
        Task MoveActiveCoupleToLeaderboardAsync();
        Task ResetInteractionPointsAsync();
    }
}
