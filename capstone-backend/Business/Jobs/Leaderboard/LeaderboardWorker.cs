using capstone_backend.Business.Interfaces;
using capstone_backend.Data.Entities;
using capstone_backend.Data.Enums;
using Microsoft.EntityFrameworkCore;

namespace capstone_backend.Business.Jobs.Leaderboard
{
    public class LeaderboardWorker : ILeaderboardWorker
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<LeaderboardWorker> _logger;

        public LeaderboardWorker(IUnitOfWork unitOfWork, ILogger<LeaderboardWorker> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task MoveActiveCoupleToLeaderboardAsync()
        {
            var now = DateTime.UtcNow;
            var oneMonthAgo = now.AddMonths(-1);
            var currentSeasonKey = $"{now.Year}-{now.Month:D2}";
            var periodStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            var periodEnd = periodStart.AddMonths(1).AddTicks(-1);

            // Lấy tất cả couple ACTIVE >= 1 tháng
            var couples = await _unitOfWork.Context.CoupleProfiles
                .Where(c => c.Status == CoupleProfileStatus.ACTIVE.ToString() 
                         && c.IsDeleted != true
                         && c.StartDate.HasValue
                         && c.StartDate.Value <= DateOnly.FromDateTime(oneMonthAgo))
                .ToListAsync();

            // Check couple nào chưa có leaderboard THÁNG NÀY
            var coupleIds = couples.Select(c => c.id).ToList();
            var existingInCurrentMonth = await _unitOfWork.Context.Leaderboards
                .Where(l => coupleIds.Contains(l.CoupleId) 
                         && l.SeasonKey == currentSeasonKey)
                .Select(l => l.CoupleId)
                .ToListAsync();

            var newCouples = couples.Where(c => !existingInCurrentMonth.Contains(c.id)).ToList();

            // Tạo leaderboard tháng mới
            foreach (var couple in newCouples)
            {
                var leaderboard = new Data.Entities.Leaderboard
                {
                    CoupleId = couple.id,
                    PeriodType = "monthly",
                    PeriodStart = periodStart,
                    PeriodEnd = periodEnd,
                    SeasonKey = currentSeasonKey,
                    TotalPoints = couple.RankingPoints ?? 0,
                    RankPosition = 0,
                    Status = LeaderboardStatus.ACTIVE.ToString(),
                    UpdatedAt = now
                };

                await _unitOfWork.Context.Leaderboards.AddAsync(leaderboard);
            }

            await _unitOfWork.SaveChangesAsync();

            await RecalculateMonthlyUniqueRankPositionAsync(currentSeasonKey, now);

            _logger.LogInformation($"Đã tạo leaderboard tháng {currentSeasonKey} cho {newCouples.Count} couple");
        }

        private async Task RecalculateMonthlyUniqueRankPositionAsync(string seasonKey, DateTime now)
        {
            var monthlyRows = await _unitOfWork.Context.Leaderboards
                .Where(l => l.PeriodType == "monthly"
                         && l.SeasonKey == seasonKey
                         && l.Status == LeaderboardStatus.ACTIVE.ToString())
                .OrderByDescending(l => l.TotalPoints ?? 0)
                .ThenBy(l => l.UpdatedAt)
                .ThenBy(l => l.Id)
                .ToListAsync();

            if (!monthlyRows.Any())
                return;

            var hasChanges = false;

            for (int i = 0; i < monthlyRows.Count; i++)
            {
                var expectedRank = i + 1; // unique rank
                var row = monthlyRows[i];

                if (row.RankPosition != expectedRank)
                {
                    row.RankPosition = expectedRank;
                    row.UpdatedAt = now;
                    hasChanges = true;
                }
            }

            if (hasChanges)
                await _unitOfWork.SaveChangesAsync();
        }

        public async Task ResetInteractionPointsAsync()
        {
            var now = DateTime.UtcNow;
            var threeMonthsAgo = now.AddMonths(-3);

            // Tìm couple đã có trong leaderboard ACTIVE >= 3 tháng
            // Chỉ lấy leaderboard ACTIVE để tránh reset couple đã break up
            var oldCoupleIds = await _unitOfWork.Context.Leaderboards
                .Where(l => l.PeriodStart <= threeMonthsAgo 
                         && l.Status == LeaderboardStatus.ACTIVE.ToString())
                .GroupBy(l => l.CoupleId)
                .Select(g => g.Key)
                .ToListAsync();

            var couples = await _unitOfWork.Context.CoupleProfiles
                .Where(c => oldCoupleIds.Contains(c.id) 
                         && c.Status == CoupleProfileStatus.ACTIVE.ToString()
                         && c.IsDeleted != true)
                .ToListAsync();

            foreach (var couple in couples)
            {
                couple.RankingPoints = 0;
                couple.UpdatedAt = now;
            }

            await _unitOfWork.SaveChangesAsync();
            _logger.LogInformation($"Đã reset interaction points cho {couples.Count} couple (trong leaderboard >= 3 tháng)");
        }
    }
}
