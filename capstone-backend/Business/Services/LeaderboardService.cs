using capstone_backend.Business.DTOs.Leaderboard;
using capstone_backend.Business.Interfaces;
using capstone_backend.Data.Enums;
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

        // Tính ngày đầu và cuối tháng chính xác
        var periodStart = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
        var nextMonth = periodStart.AddMonths(1);
        var periodEnd = nextMonth.AddTicks(-1); // Giây cuối cùng của tháng

        var (leaderboards, totalCount) = await _unitOfWork.Leaderboards.GetLeaderboardByPeriodAsync(
            "monthly",
            periodStart,
            periodEnd,
            pageNumber,
            pageSize
        );

        if (!leaderboards.Any())
            return null;

        var firstItem = leaderboards.First();

        // Query couple với member info
        var coupleIds = leaderboards.Select(l => l.CoupleId).ToList();
        var couples = await _unitOfWork.Context.CoupleProfiles
            .Include(c => c.MemberId1Navigation)
                .ThenInclude(m => m.User)
            .Include(c => c.MemberId2Navigation)
                .ThenInclude(m => m.User)
            .Where(c => coupleIds.Contains(c.id))
            .ToListAsync();

        var coupleDict = couples.ToDictionary(c => c.id);

        return new LeaderboardListResponse
        {
            PeriodType = "monthly",
            SeasonKey = firstItem.SeasonKey,
            PeriodStart = firstItem.PeriodStart,
            PeriodEnd = firstItem.PeriodEnd,
            TotalCount = totalCount,
            Rankings = leaderboards.Select(l =>
            {
                var couple = coupleDict.GetValueOrDefault(l.CoupleId);
                return new LeaderboardResponse
                {
                    Id = l.Id,
                    CoupleId = l.CoupleId,
                    CoupleName = couple?.CoupleName ?? "Unknown",
                    Member1 = couple?.MemberId1Navigation != null ? new MemberInfo
                    {
                        MemberId = couple.MemberId1Navigation.Id,
                        MemberName = couple.MemberId1Navigation.FullName,
                        AvatarUrl = couple.MemberId1Navigation.User?.AvatarUrl
                    } : null,
                    Member2 = couple?.MemberId2Navigation != null ? new MemberInfo
                    {
                        MemberId = couple.MemberId2Navigation.Id,
                        MemberName = couple.MemberId2Navigation.FullName,
                        AvatarUrl = couple.MemberId2Navigation.User?.AvatarUrl
                    } : null,
                    TotalPoints = l.TotalPoints,
                    RankPosition = l.RankPosition,
                    UpdatedAt = l.UpdatedAt
                };
            }).ToList()
        };
    }

    public async Task AddCoupleToLeaderboardAsync(int coupleId)
    {
        var now = DateTime.UtcNow;
        var currentSeasonKey = $"{now.Year}-{now.Month:D2}";

        // Check xem couple đã có trong leaderboard tháng này chưa
        var exists = await _unitOfWork.Context.Leaderboards
            .AnyAsync(l => l.CoupleId == coupleId 
                        && l.SeasonKey == currentSeasonKey);

        if (exists)
            return; // Đã có rồi, không thêm nữa

        var leaderboard = new Data.Entities.Leaderboard
        {
            CoupleId = coupleId,
            PeriodType = "monthly",
            PeriodStart = now,
            PeriodEnd = now.AddMonths(1),
            SeasonKey = currentSeasonKey,
            TotalPoints = 0,
            RankPosition = 0,
            Status = LeaderboardStatus.ACTIVE.ToString(),
            UpdatedAt = now
        };

        await _unitOfWork.Context.Leaderboards.AddAsync(leaderboard);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task RemoveCoupleFromLeaderboardAsync(int coupleId)
    {
        var leaderboards = await _unitOfWork.Context.Leaderboards
            .Where(l => l.CoupleId == coupleId && l.Status == LeaderboardStatus.ACTIVE.ToString())
            .ToListAsync();

        foreach (var leaderboard in leaderboards)
        {
            leaderboard.Status = LeaderboardStatus.INACTIVE.ToString();
            leaderboard.UpdatedAt = DateTime.UtcNow;
        }

        await _unitOfWork.SaveChangesAsync();
    }
}
