using capstone_backend.Business.Common.Constants;
using capstone_backend.Business.DTOs.VenueLocation;
using capstone_backend.Business.Interfaces;
using capstone_backend.Data.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace capstone_backend.Business.Services;

/// <summary>
/// Service để phân tích độ chính xác của venue tags
/// Dựa trên CoupleMoodSnapshot trong reviews để đánh giá tag nào phù hợp/không phù hợp
/// </summary>
public class VenueTagAnalysisService : IVenueTagAnalysisService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ISystemConfigService _systemConfigService;
    private readonly ILogger<VenueTagAnalysisService> _logger;

    public VenueTagAnalysisService(
        IUnitOfWork unitOfWork,
        ISystemConfigService systemConfigService,
        ILogger<VenueTagAnalysisService> logger)
    {
        _unitOfWork = unitOfWork;
        _systemConfigService = systemConfigService;
        _logger = logger;
    }

    /// <summary>
    /// Phân tích độ chính xác của tất cả tags của venue
    /// </summary>
    public async Task<VenueTagAnalysisResponse> AnalyzeVenueTagsAsync(int venueId)
    {
        // Load thresholds từ SystemConfig
        var goodThreshold = await GetGoodThresholdAsync();
        var warningThreshold = await GetWarningThresholdAsync();
        var minReviews = await GetMinReviewsAsync();

        // Lấy venue info
        var venue = await _unitOfWork.VenueLocations.GetByIdAsync(venueId);
        if (venue == null)
        {
            throw new Exception("Không tìm thấy venue");
        }

        // Lấy tất cả tags của venue
        var venueTags = await GetVenueTagsAsync(venueId);
        
        // Lấy tất cả published reviews
        var allReviews = await _unitOfWork.Context.Set<Data.Entities.Review>()
            .Where(r => r.VenueId == venueId 
                && r.Status == "PUBLISHED" 
                && r.IsDeleted != true)
            .ToListAsync();

        var tagAnalysisList = new List<TagAccuracyDetail>();

        // Phân tích từng tag
        foreach (var tag in venueTags)
        {
            var analysis = AnalyzeTag(tag, allReviews, goodThreshold, warningThreshold, minReviews);
            tagAnalysisList.Add(analysis);
        }

        // Tính overall match rate
        var totalReviewsWithSnapshot = allReviews.Count(r => !string.IsNullOrEmpty(r.CoupleMoodSnapshot));
        var overallMatchRate = totalReviewsWithSnapshot > 0
            ? (decimal)allReviews.Count(r => r.IsMatched == true) / totalReviewsWithSnapshot * 100
            : 0;

        // Tạo summary
        var summary = CreateSummary(tagAnalysisList);

        return new VenueTagAnalysisResponse
        {
            VenueId = venueId,
            VenueName = venue.Name,
            OverallMatchRate = Math.Round(overallMatchRate, 1),
            TotalReviews = allReviews.Count,
            TagAnalysis = tagAnalysisList,
            Summary = summary
        };
    }

    /// <summary>
    /// Phân tích một tag cụ thể
    /// </summary>
    private TagAccuracyDetail AnalyzeTag(
        VenueTagInfo tag, 
        List<Data.Entities.Review> allReviews,
        decimal goodThreshold,
        decimal warningThreshold,
        int minReviews)
    {
        // Lọc reviews có CoupleMoodSnapshot chứa tag này
        var relevantReviews = allReviews
            .Where(r => !string.IsNullOrEmpty(r.CoupleMoodSnapshot) 
                && r.CoupleMoodSnapshot.Contains(tag.Name, StringComparison.OrdinalIgnoreCase))
            .ToList();

        var totalReviews = relevantReviews.Count;

        // Chưa đủ data
        if (totalReviews < minReviews)
        {
            return new TagAccuracyDetail
            {
                Tag = tag.Name,
                TagType = tag.Type,
                Status = VenueTagAnalysisConstants.STATUS_INSUFFICIENT_DATA,
                Severity = VenueTagAnalysisConstants.SEVERITY_NONE,
                TotalReviews = totalReviews,
                MatchedCount = 0,
                UnmatchedCount = 0,
                MatchRate = 0,
                Message = $"Chưa đủ dữ liệu để đánh giá (cần ít nhất {minReviews} reviews)",
                Recommendation = null
            };
        }

        // Đếm matched vs unmatched
        var matchedCount = relevantReviews.Count(r => r.IsMatched == true);
        var unmatchedCount = relevantReviews.Count(r => r.IsMatched == false);
        var matchRate = (decimal)matchedCount / totalReviews * 100;

        // Xác định status và severity
        string status, severity, recommendation;
        if (matchRate >= goodThreshold)
        {
            status = VenueTagAnalysisConstants.STATUS_GOOD;
            severity = VenueTagAnalysisConstants.SEVERITY_NONE;
            recommendation = VenueTagAnalysisConstants.RECOMMENDATION_KEEP;
        }
        else if (matchRate >= warningThreshold)
        {
            status = VenueTagAnalysisConstants.STATUS_WARNING;
            severity = VenueTagAnalysisConstants.SEVERITY_MEDIUM;
            recommendation = VenueTagAnalysisConstants.RECOMMENDATION_REVIEW;
        }
        else
        {
            status = VenueTagAnalysisConstants.STATUS_POOR;
            severity = VenueTagAnalysisConstants.SEVERITY_HIGH;
            recommendation = VenueTagAnalysisConstants.RECOMMENDATION_REMOVE;
        }

        var message = GenerateMessage(tag.Name, matchRate, status);

        return new TagAccuracyDetail
        {
            Tag = tag.Name,
            TagType = tag.Type,
            Status = status,
            Severity = severity,
            TotalReviews = totalReviews,
            MatchedCount = matchedCount,
            UnmatchedCount = unmatchedCount,
            MatchRate = Math.Round(matchRate, 1),
            Message = message,
            Recommendation = recommendation
        };
    }

    /// <summary>
    /// Tạo message dựa trên status
    /// </summary>
    private string GenerateMessage(string tagName, decimal matchRate, string status)
    {
        return status switch
        {
            VenueTagAnalysisConstants.STATUS_GOOD => 
                $"Tag '{tagName}' phù hợp với venue ({matchRate:F1}% khách hàng hài lòng)",
            
            VenueTagAnalysisConstants.STATUS_WARNING => 
                $"Tag '{tagName}' có vẻ không hoàn toàn phù hợp (chỉ {matchRate:F1}% khách hàng hài lòng). Xem xét điều chỉnh.",
            
            VenueTagAnalysisConstants.STATUS_POOR => 
                $"⚠️ Tag '{tagName}' KHÔNG phù hợp với venue (chỉ {matchRate:F1}% khách hàng hài lòng). Nên xóa tag này!",
            
            _ => $"Tag '{tagName}' chưa có đủ dữ liệu để đánh giá"
        };
    }

    /// <summary>
    /// Tạo summary từ danh sách phân tích
    /// </summary>
    private TagAnalysisSummary CreateSummary(List<TagAccuracyDetail> tagAnalysisList)
    {
        var goodTags = tagAnalysisList
            .Where(t => t.Status == VenueTagAnalysisConstants.STATUS_GOOD)
            .Select(t => t.Tag)
            .ToList();

        var warningTags = tagAnalysisList
            .Where(t => t.Status == VenueTagAnalysisConstants.STATUS_WARNING)
            .Select(t => t.Tag)
            .ToList();

        var poorTags = tagAnalysisList
            .Where(t => t.Status == VenueTagAnalysisConstants.STATUS_POOR)
            .Select(t => t.Tag)
            .ToList();

        var actionRequired = warningTags.Any() || poorTags.Any();

        string overallMessage;
        string impactMessage = null;
        
        if (poorTags.Any())
        {
            overallMessage = $"Venue có {poorTags.Count} tag không phù hợp cần xóa";
            impactMessage = "Giữ nguyên các tag không phù hợp sẽ làm giảm khả năng venue được gợi ý cho khách hàng phù hợp, dẫn đến reviews tiêu cực và ảnh hưởng xếp hạng.";
        }
        else if (warningTags.Any())
        {
            overallMessage = $"Venue có {warningTags.Count} tag cần xem xét điều chỉnh";
            impactMessage = "Các tag này có thể làm giảm độ chính xác khi hệ thống gợi ý venue cho khách hàng, nên xem xét cải thiện hoặc xóa bỏ.";
        }
        else if (goodTags.Any())
        {
            overallMessage = "Tất cả tags đều phù hợp với venue";
        }
        else
        {
            overallMessage = "Chưa đủ dữ liệu để đánh giá";
        }

        return new TagAnalysisSummary
        {
            GoodTags = goodTags,
            WarningTags = warningTags,
            PoorTags = poorTags,
            ActionRequired = actionRequired,
            OverallMessage = overallMessage,
            ImpactMessage = impactMessage
        };
    }

    /// <summary>
    /// Lấy tất cả tags của venue
    /// </summary>
    private async Task<List<VenueTagInfo>> GetVenueTagsAsync(int venueId)
    {
        var venueLocationTags = await _unitOfWork.Context.Set<Data.Entities.VenueLocationTag>()
            .Include(vlt => vlt.LocationTag)
                .ThenInclude(lt => lt!.CoupleMoodType)
            .Include(vlt => vlt.LocationTag)
                .ThenInclude(lt => lt!.CouplePersonalityType)
            .Where(vlt => vlt.VenueLocationId == venueId && vlt.IsDeleted != true)
            .ToListAsync();

        var tags = new List<VenueTagInfo>();

        foreach (var vlt in venueLocationTags)
        {
            if (vlt.LocationTag?.CoupleMoodType != null)
            {
                tags.Add(new VenueTagInfo
                {
                    Name = vlt.LocationTag.CoupleMoodType.Name,
                    Type = "CoupleMoodType"
                });
            }

            if (vlt.LocationTag?.CouplePersonalityType != null)
            {
                tags.Add(new VenueTagInfo
                {
                    Name = vlt.LocationTag.CouplePersonalityType.Name,
                    Type = "CouplePersonalityType"
                });
            }
        }

        return tags.DistinctBy(t => t.Name).ToList();
    }

    /// <summary>
    /// Load threshold từ SystemConfig
    /// </summary>
    private async Task<decimal> GetGoodThresholdAsync()
    {
        try
        {
            return await _systemConfigService.GetDecimalValueAsync(
                VenueTagAnalysisConstants.GOOD_THRESHOLD_KEY);
        }
        catch
        {
            _logger.LogWarning($"Using default GOOD_THRESHOLD: {VenueTagAnalysisConstants.DEFAULT_GOOD_THRESHOLD}");
            return VenueTagAnalysisConstants.DEFAULT_GOOD_THRESHOLD;
        }
    }

    private async Task<decimal> GetWarningThresholdAsync()
    {
        try
        {
            return await _systemConfigService.GetDecimalValueAsync(
                VenueTagAnalysisConstants.WARNING_THRESHOLD_KEY);
        }
        catch
        {
            _logger.LogWarning($"Using default WARNING_THRESHOLD: {VenueTagAnalysisConstants.DEFAULT_WARNING_THRESHOLD}");
            return VenueTagAnalysisConstants.DEFAULT_WARNING_THRESHOLD;
        }
    }

    private async Task<int> GetMinReviewsAsync()
    {
        try
        {
            return await _systemConfigService.GetIntValueAsync(
                VenueTagAnalysisConstants.MIN_REVIEWS_KEY);
        }
        catch
        {
            _logger.LogWarning($"Using default MIN_REVIEWS: {VenueTagAnalysisConstants.DEFAULT_MIN_REVIEWS}");
            return VenueTagAnalysisConstants.DEFAULT_MIN_REVIEWS;
        }
    }

    /// <summary>
    /// Helper class để lưu thông tin tag
    /// </summary>
    private class VenueTagInfo
    {
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
    }
}
