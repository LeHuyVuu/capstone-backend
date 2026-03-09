using capstone_backend.Business.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace capstone_backend.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class InsightController : BaseController
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<InsightController> _logger;

    public InsightController(IUnitOfWork unitOfWork, ILogger<InsightController> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <summary>
    /// Get venue insights including top searches, hot moods, popular personalities, and mood trends
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetVenueInsights([FromQuery] string? timeframe = "all")
    {
        try
        {
            var now = DateTime.UtcNow;
            DateTime? startDate = timeframe?.ToLower() switch
            {
                "today" => now.Date,
                "week" => now.AddDays(-7),
                "month" => now.AddMonths(-1),
                "year" => now.AddYears(-1),
                _ => null
            };

            // Top Searches
            var topSearchesQuery = _unitOfWork.Context.SearchHistories
                .Where(sh => !sh.IsDeleted.HasValue || !sh.IsDeleted.Value);

            if (startDate.HasValue)
                topSearchesQuery = topSearchesQuery.Where(sh => sh.SearchedAt >= startDate.Value);

            var topSearches = await topSearchesQuery
                .GroupBy(sh => sh.Keyword)
                .Select(g => new { Keyword = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .Take(10)
                .ToListAsync();

            var totalSearches = topSearches.Sum(x => x.Count);
            var topSearchInsights = topSearches.Select(ts => new
            {
                Keyword = ts.Keyword,
                Count = ts.Count,
                Percentage = totalSearches > 0 ? Math.Round((double)ts.Count / totalSearches * 100, 0) : 0
            }).Take(3).ToList();

            // Hot Moods - from MemberMoodLog
            var hotMoodQuery = _unitOfWork.Context.MemberMoodLogs
                .Where(mml => (!mml.IsDeleted.HasValue || !mml.IsDeleted.Value));

            if (startDate.HasValue)
                hotMoodQuery = hotMoodQuery.Where(mml => mml.CreatedAt >= startDate.Value);

            var hotMoods = await hotMoodQuery
                .Include(mml => mml.MoodType)
                .GroupBy(mml => new { mml.MoodTypeId, mml.MoodType.Name })
                .Select(g => new { MoodTypeId = g.Key.MoodTypeId, MoodName = g.Key.Name, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .Take(10)
                .ToListAsync();

            var totalMoodLogs = hotMoods.Sum(x => x.Count);
            var hotMoodInsights = hotMoods.Select(hm => new
            {
                MoodTypeId = hm.MoodTypeId,
                MoodName = hm.MoodName,
                Count = hm.Count,
                Percentage = totalMoodLogs > 0 ? Math.Round((double)hm.Count / totalMoodLogs * 100, 0) : 0
            }).Take(3).ToList();

            // Mood Trends by Month (last 12 months)
            var yearAgo = now.AddYears(-1);
            var moodTrends = await _unitOfWork.Context.CoupleMoodLogs
                .Where(cml => (!cml.IsDeleted.HasValue || !cml.IsDeleted.Value) && cml.CreatedAt >= yearAgo)
                .Include(cml => cml.CoupleMoodType)
                .GroupBy(cml => new { 
                    Month = new DateTime(cml.CreatedAt!.Value.Year, cml.CreatedAt.Value.Month, 1),
                    MoodName = cml.CoupleMoodType.Name
                })
                .Select(g => new
                {
                    Month = g.Key.Month,
                    MoodName = g.Key.MoodName,
                    Count = g.Count()
                })
                .OrderBy(x => x.Month)
                .ToListAsync();

            var moodTrendsByMonth = moodTrends
                .GroupBy(mt => mt.Month)
                .Select(g => new
                {
                    Month = g.Key.ToString("yyyy-MM"),
                    MonthName = g.Key.ToString("MMM"),
                    Moods = g.Select(x => new { MoodName = x.MoodName, Count = x.Count }).ToList(),
                    TotalCount = g.Sum(x => x.Count)
                })
                .OrderBy(x => x.Month)
                .ToList();

            // ========================================
            // FAVORITES & INTERACTIONS STATISTICS
            // ========================================
            
            // Base interaction query with timeframe filter
            var baseInteractionQuery = _unitOfWork.Context.Interactions.AsQueryable();
            if (startDate.HasValue)
                baseInteractionQuery = baseInteractionQuery.Where(i => i.CreatedAt >= startDate.Value);

            // 1. TOP VENUE CATEGORIES BY INTERACTIONS
            var venueInteractionQuery = baseInteractionQuery
                .Where(i => i.TargetType == "VenueLocation" && i.TargetId.HasValue);

            // Fetch data first, then process in memory
            var venueInteractionData = await venueInteractionQuery
                .Join(_unitOfWork.Context.VenueLocations,
                    i => i.TargetId!.Value,
                    v => v.Id,
                    (i, v) => new { i.InteractionType, v.Category, i.MemberId })
                .Where(x => !string.IsNullOrEmpty(x.Category))
                .ToListAsync();

            // Process category splits in memory
            var topVenueCategories = venueInteractionData
                .SelectMany(x => x.Category!.Split(new[] { " / ", "/" }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(cat => new { Category = cat.Trim(), x.InteractionType, x.MemberId }))
                .GroupBy(x => x.Category)
                .Select(g => new
                {
                    Category = g.Key,
                    TotalInteractions = g.Count(),
                    UniqueUsers = g.Select(x => x.MemberId).Distinct().Count(),
                    InteractionBreakdown = g.GroupBy(x => x.InteractionType ?? "UNKNOWN")
                        .Select(ig => new { Type = ig.Key, Count = ig.Count() })
                        .ToList()
                })
                .OrderByDescending(x => x.TotalInteractions)
                .Take(5)
                .ToList();

            // 2. ADVERTISEMENT PERFORMANCE BY CATEGORY
            var adPerformanceQuery = baseInteractionQuery
                .Where(i => i.TargetType == "Advertisement" && i.TargetId.HasValue)
                .Join(_unitOfWork.Context.Advertisements,
                    i => i.TargetId!.Value,
                    a => a.Id,
                    (i, a) => new { i.InteractionType, a.Category, a.PlacementType, i.MemberId });

            var adPerformanceByCategory = await adPerformanceQuery
                .Where(x => !string.IsNullOrEmpty(x.Category))
                .GroupBy(x => new { x.Category, x.PlacementType })
                .Select(g => new
                {
                    Category = g.Key.Category,
                    PlacementType = g.Key.PlacementType,
                    Views = g.Count(x => x.InteractionType == "VIEW"),
                    Clicks = g.Count(x => x.InteractionType == "CLICK"),
                    UniqueViewers = g.Where(x => x.InteractionType == "VIEW")
                        .Select(x => x.MemberId).Distinct().Count(),
                    CTR = g.Count(x => x.InteractionType == "VIEW") > 0
                        ? Math.Round((double)g.Count(x => x.InteractionType == "CLICK") / 
                            g.Count(x => x.InteractionType == "VIEW") * 100, 2)
                        : 0
                })
                .OrderByDescending(x => x.Clicks)
                .ThenByDescending(x => x.Views)
                .ToListAsync();

            // 5. CHECK-IN STATISTICS
            var checkInQuery = _unitOfWork.Context.CheckInHistories
                .Where(ch => ch.IsValid == true);

            if (startDate.HasValue)
                checkInQuery = checkInQuery.Where(ch => ch.CreatedAt >= startDate.Value);

            var topCheckInVenues = await checkInQuery
                .GroupBy(ch => ch.VenueId)
                .Select(g => new { VenueId = g.Key, CheckInCount = g.Count() })
                .OrderByDescending(x => x.CheckInCount)
                .Take(10)
                .ToListAsync();

            var topCheckInVenueIds = topCheckInVenues.Select(tc => tc.VenueId).ToList();
            var topCheckInVenueDetails = await _unitOfWork.Context.VenueLocations
                .Where(v => topCheckInVenueIds.Contains(v.Id) && v.IsDeleted != true)
                .Select(v => new { v.Id, v.Name, v.Category, v.AverageRating, v.Status })
                .ToListAsync();

            var topCheckInVenuesWithDetails = topCheckInVenues
                .Join(topCheckInVenueDetails, tc => tc.VenueId, v => v.Id, (tc, v) => new
                {
                    VenueId = v.Id,
                    VenueName = v.Name,
                    Category = v.Category,
                    CheckInCount = tc.CheckInCount,
                    AverageRating = v.AverageRating,
                    Status = v.Status
                })
                .Take(5)
                .ToList();

            var totalCheckIns = await checkInQuery.CountAsync();

            var result = new
            {
                Timeframe = timeframe ?? "all",
                GeneratedAt = now,
                TopSearches = topSearchInsights,
                HotMoods = hotMoodInsights,
                MoodTrendsByMonth = moodTrendsByMonth,
                FavoritesAndInteractions = new
                {
                    TopVenueCategories = topVenueCategories,
                    AdPerformanceByCategory = adPerformanceByCategory,
                    TopCheckInVenues = topCheckInVenuesWithDetails,
                    TotalCheckIns = totalCheckIns
                }
            };

            return OkResponse(result, "Insights retrieved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving venue insights");
            return InternalServerErrorResponse("Failed to retrieve insights");
        }
    }
}
