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

            // Popular Preferences - from Interactions with LocationTags
            var interactionQuery = _unitOfWork.Context.Interactions
                .Where(i => i.TargetType == "VenueLocation");

            if (startDate.HasValue)
                interactionQuery = interactionQuery.Where(i => i.CreatedAt >= startDate.Value);

            var interactionTags = await interactionQuery
                .Join(_unitOfWork.Context.VenueLocations
                        .Include(v => v.VenueLocationTags)
                            .ThenInclude(vlt => vlt.LocationTag),
                    i => i.TargetId,
                    v => v.Id,
                    (i, v) => v)
                .SelectMany(v => v.VenueLocationTags)
                .Where(vlt => !vlt.IsDeleted.HasValue || !vlt.IsDeleted.Value)
                .Select(vlt => vlt.LocationTag.DetailTag)
                .Where(tags => tags != null)
                .ToListAsync();

            // Flatten all tags and count
            var tagCounts = interactionTags
                .SelectMany(tags => tags!)
                .GroupBy(tag => tag)
                .Select(g => new { Tag = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .Take(10)
                .ToList();

            var totalTags = tagCounts.Sum(x => x.Count);
            var popularPreferenceInsights = tagCounts.Select(tc => new
            {
                Tag = tc.Tag,
                Count = tc.Count,
                Percentage = totalTags > 0 ? Math.Round((double)tc.Count / totalTags * 100, 0) : 0
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

            var result = new
            {
                Timeframe = timeframe ?? "all",
                GeneratedAt = now,
                TopSearches = topSearchInsights,
                HotMoods = hotMoodInsights,
                PopularPreferences = popularPreferenceInsights,
                MoodTrendsByMonth = moodTrendsByMonth
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
