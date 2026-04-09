using capstone_backend.Api.Filters;
using capstone_backend.Business.Interfaces;
using capstone_backend.Business.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenAI.Chat;

namespace capstone_backend.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class InsightController : BaseController
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<InsightController> _logger;
    private readonly IConfiguration _configuration;
    private readonly IRedisService _redisService;

    public InsightController(
        IUnitOfWork unitOfWork, 
        ILogger<InsightController> logger, 
        IConfiguration configuration,
        IRedisService redisService)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _configuration = configuration;
        _redisService = redisService;
    }

    /// <summary>
    /// Get venue insights including top searches, hot moods, popular personalities, and mood trends
    /// Cached in Redis for 30 minutes
    /// </summary>
    [RequireActiveSubscription(UserType = "VENUEOWNER", FeatureCode = "VENUE_INSIGHT", ErrorMessage = "Bạn cần gia hạn gói để dùng tính năng này", ErrorStatusCode = 402)]
    [HttpGet]
    public async Task<IActionResult> GetVenueInsights([FromQuery] string? timeframe = "all")
    {
        try
        {
            var cacheKey = $"insights:venue:{timeframe ?? "all"}";
            
            // Try to get from Redis cache
            var cachedResult = await _redisService.GetOrSetAsync(
                cacheKey,
                async () => await GenerateInsightsAsync(timeframe),
                TimeSpan.FromMinutes(30)
            );

            if (cachedResult != null)
            {
                _logger.LogInformation("Insights retrieved from cache for timeframe: {Timeframe}", timeframe ?? "all");
                return OkResponse(cachedResult, "Lấy dữ liệu thống kê thành công (từ bộ nhớ đệm)");
            }

            return InternalServerErrorResponse("Không thể lấy dữ liệu thống kê");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving venue insights");
            return InternalServerErrorResponse("Không thể lấy dữ liệu thống kê");
        }
    }

    private async Task<object> GenerateInsightsAsync(string? timeframe)
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

        var topSearches = await _unitOfWork.Context.SearchHistories
            .Where(sh => (!sh.IsDeleted.HasValue || !sh.IsDeleted.Value) && (!startDate.HasValue || sh.SearchedAt >= startDate.Value))
            .GroupBy(sh => sh.Keyword)
            .Select(g => new { Keyword = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Take(10)
            .ToListAsync();

        var totalSearches = topSearches.Sum(x => x.Count);
        var topSearchInsights = topSearches.Take(3).Select(ts => new { ts.Keyword, ts.Count, Percentage = totalSearches > 0 ? Math.Round((double)ts.Count / totalSearches * 100, 0) : 0 }).ToList();

        var hotMoods = await _unitOfWork.Context.MemberMoodLogs
            .Where(mml => (!mml.IsDeleted.HasValue || !mml.IsDeleted.Value) && (!startDate.HasValue || mml.CreatedAt >= startDate.Value))
            .GroupBy(mml => new { mml.MoodTypeId, MoodName = mml.MoodType.Name })
            .Select(g => new { g.Key.MoodTypeId, g.Key.MoodName, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Take(10)
            .ToListAsync();

        var totalMoodLogs = hotMoods.Sum(x => x.Count);
        var hotMoodInsights = hotMoods
            .Take(3)
            .Select(hm => new
            {
                hm.MoodTypeId,
                MoodName = FaceEmotionService.MapEmotionToVietnamese(hm.MoodName),
                hm.Count,
                Percentage = totalMoodLogs > 0 ? Math.Round((double)hm.Count / totalMoodLogs * 100, 0) : 0
            })
            .ToList();

        var yearAgo = now.AddYears(-1);
        var moodTrends = await _unitOfWork.Context.CoupleMoodLogs
            .Where(cml => (!cml.IsDeleted.HasValue || !cml.IsDeleted.Value) && cml.CreatedAt >= yearAgo)
            .GroupBy(cml => new { 
                Month = new DateTime(cml.CreatedAt!.Value.Year, cml.CreatedAt.Value.Month, 1),
                MoodName = cml.CoupleMoodType.Name
            })
            .Select(g => new { g.Key.Month, g.Key.MoodName, Count = g.Count() })
            .OrderBy(x => x.Month)
            .ToListAsync();

        var moodTrendsByMonth = moodTrends.GroupBy(mt => mt.Month).Select(g => new { Month = g.Key.ToString("yyyy-MM"), MonthName = g.Key.ToString("MMM"), Moods = g.Select(x => new { x.MoodName, x.Count }).ToList(), TotalCount = g.Sum(x => x.Count) }).OrderBy(x => x.Month).ToList();

        var venueInteractionData = await _unitOfWork.Context.Interactions
            .Where(i => i.TargetType == "VenueLocation" && i.TargetId.HasValue && (!startDate.HasValue || i.CreatedAt >= startDate.Value))
            .Join(_unitOfWork.Context.VenueLocations, i => i.TargetId!.Value, v => v.Id, (i, v) => new { i.InteractionType, v.Category, i.MemberId })
            .Where(x => !string.IsNullOrEmpty(x.Category))
            .ToListAsync();

        var topVenueCategories = venueInteractionData
            .SelectMany(x => x.Category!.Split(new[] { " / ", "/" }, StringSplitOptions.RemoveEmptyEntries).Select(cat => new { Category = cat.Trim(), x.InteractionType, x.MemberId }))
            .GroupBy(x => x.Category)
            .Select(g => new { Category = g.Key, TotalInteractions = g.Count(), UniqueUsers = g.Select(x => x.MemberId).Distinct().Count(), InteractionBreakdown = g.GroupBy(x => x.InteractionType ?? "UNKNOWN").Select(ig => new { Type = ig.Key, Count = ig.Count() }).ToList() })
            .OrderByDescending(x => x.TotalInteractions)
            .Take(5)
            .ToList();

        var adPerformanceByCategory = await _unitOfWork.Context.Interactions
            .Where(i => i.TargetType == "Advertisement" && i.TargetId.HasValue && (!startDate.HasValue || i.CreatedAt >= startDate.Value))
            .Join(_unitOfWork.Context.Advertisements, i => i.TargetId!.Value, a => a.Id, (i, a) => new { i.InteractionType, a.Category, a.PlacementType, i.MemberId })
            .Where(x => !string.IsNullOrEmpty(x.Category))
            .GroupBy(x => new { x.Category, x.PlacementType })
            .Select(g => new
            {
                g.Key.Category,
                g.Key.PlacementType,
                Views = g.Count(x => x.InteractionType == "VIEW"),
                Clicks = g.Count(x => x.InteractionType == "CLICK"),
                UniqueViewers = g.Where(x => x.InteractionType == "VIEW").Select(x => x.MemberId).Distinct().Count(),
                CTR = g.Count(x => x.InteractionType == "VIEW") > 0 ? Math.Round((double)g.Count(x => x.InteractionType == "CLICK") / g.Count(x => x.InteractionType == "VIEW") * 100, 2) : 0
            })
            .OrderByDescending(x => x.Clicks)
            .ThenByDescending(x => x.Views)
            .ToListAsync();

        var topCheckInVenues = await _unitOfWork.Context.CheckInHistories
            .Where(ch => ch.IsValid == true && (!startDate.HasValue || ch.CreatedAt >= startDate.Value))
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

        var topCheckInVenuesWithDetails = topCheckInVenues.Join(topCheckInVenueDetails, tc => tc.VenueId, v => v.Id, (tc, v) => new { v.Id, v.Name, v.Category, tc.CheckInCount, v.AverageRating, v.Status }).Take(5).ToList();
        var totalCheckIns = topCheckInVenues.Sum(x => x.CheckInCount);

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

        var apiKey = _configuration["OPENAI_API_KEY"];
        var modelName = _configuration["MODEL_NAME"] ?? "gpt-4o-mini";
        var client = new ChatClient(modelName, apiKey);
        var prompt = $@"Phân tích dữ liệu insight và trả về ĐÚNG định dạng JSON (không có markdown, không có ```json):
{{
  ""searchTrends"": {{
    ""summary"": ""Tóm tắt xu hướng tìm kiếm"",
    ""topKeywords"": [{{""keyword"": ""tên từ khóa"", ""insight"": ""phân tích ngắn gọn""}}]
  }},
  ""moodAnalysis"": {{
    ""dominantMoods"": [{{""mood"": ""tên mood"", ""percentage"": số phần trăm, ""trend"": ""xu hướng""}}],
    ""monthlyTrend"": [{{""month"": ""tháng"", ""insight"": ""phân tích biến động""}}]
  }},
  ""venuePreferences"": {{
    ""topCategories"": [{{""category"": ""danh mục"", ""reason"": ""lý do phổ biến""}}],
    ""userBehavior"": ""Mô tả hành vi người dùng""
  }},
  ""checkInInsights"": {{
    ""popularVenues"": [{{""name"": ""tên địa điểm"", ""appeal"": ""điểm hấp dẫn""}}]
  }},
  ""businessStrategy"": {{
    ""recommendations"": [""Đề xuất 1"", ""Đề xuất 2""],
    ""opportunities"": [""Cơ hội 1"", ""Cơ hội 2""]
  }}
}}

Dữ liệu: {System.Text.Json.JsonSerializer.Serialize(result)}

Chỉ trả về JSON thuần, không thêm text hay markdown.";

        var completion = await client.CompleteChatAsync(prompt);
        var analysisText = completion.Value.Content[0].Text.Trim();
        if (analysisText.StartsWith("```json")) analysisText = analysisText.Substring(7);
        if (analysisText.StartsWith("```")) analysisText = analysisText.Substring(3);
        if (analysisText.EndsWith("```")) analysisText = analysisText.Substring(0, analysisText.Length - 3);
        analysisText = analysisText.Trim();

        var analysis = System.Text.Json.JsonSerializer.Deserialize<object>(analysisText);

        return new { Data = result, TrendAnalysis = analysis };
    }
}
