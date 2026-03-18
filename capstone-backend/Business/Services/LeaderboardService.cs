using capstone_backend.Business.DTOs.Leaderboard;
using capstone_backend.Business.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace capstone_backend.Business.Services;

public class LeaderboardService : ILeaderboardService
{
    private readonly IUnitOfWork _unitOfWork;

    public LeaderboardService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<LeaderboardListResponse?> GetMonthlyLeaderboardAsync(int year, int month, int pageNumber = 1, int pageSize = 50)
    {
        if (month < 1 || month > 12)
            throw new ArgumentException("Tháng phải từ 1 đến 12");

        var seasonKey = $"{year}-{month:D2}";

        var (leaderboards, totalCount) = await _unitOfWork.Leaderboards.GetLeaderboardWithCoupleAsync(
            "monthly",
            seasonKey,
            pageNumber,
            pageSize
        );

        if (!leaderboards.Any())
            return null;

        var firstItem = leaderboards.First();

        // Query couple names riêng
        var coupleIds = leaderboards.Select(l => l.CoupleId).ToList();
        var couples = await _unitOfWork.CoupleProfiles.GetAsync(
            c => coupleIds.Contains(c.id),
            null
        );
        var coupleDict = couples.ToDictionary(c => c.id, c => c.CoupleName);

        return new LeaderboardListResponse
        {
            PeriodType = "monthly",
            SeasonKey = seasonKey,
            PeriodStart = firstItem.PeriodStart,
            PeriodEnd = firstItem.PeriodEnd,
            TotalCount = totalCount,
            Rankings = leaderboards.Select(l => new LeaderboardResponse
            {
                Id = l.Id,
                CoupleId = l.CoupleId,
                CoupleName = coupleDict.GetValueOrDefault(l.CoupleId, "Unknown"),
                TotalPoints = l.TotalPoints,
                RankPosition = l.RankPosition,
                UpdatedAt = l.UpdatedAt
            }).ToList()
        };
    }
}
