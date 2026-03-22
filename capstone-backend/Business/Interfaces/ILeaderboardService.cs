using capstone_backend.Business.DTOs.Leaderboard;

namespace capstone_backend.Business.Interfaces;

public interface ILeaderboardService
{
    Task<LeaderboardListResponse?> GetMonthlyLeaderboardAsync(int year, int month, int pageNumber = 1, int pageSize = 50);
    Task AddCoupleToLeaderboardAsync(int coupleId);
    Task RemoveCoupleFromLeaderboardAsync(int coupleId);
}
