using capstone_backend.Business.Common.Constants;
using capstone_backend.Business.DTOs.VenueLocation;
using capstone_backend.Business.Interfaces;
using capstone_backend.Data.Entities;
using capstone_backend.Data.Enums;
using capstone_backend.Data.Interfaces;
using capstone_backend.Api.VenueRecommendation.Service;
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
    private readonly IMeilisearchService _meilisearchService;
    private readonly ILogger<VenueTagAnalysisService> _logger;

    public VenueTagAnalysisService(
        IUnitOfWork unitOfWork,
        ISystemConfigService systemConfigService,
        IMeilisearchService meilisearchService,
        ILogger<VenueTagAnalysisService> logger)
    {
        _unitOfWork = unitOfWork;
        _systemConfigService = systemConfigService;
        _meilisearchService = meilisearchService;
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
                && r.Status == ReviewStatus.PUBLISHED.ToString() 
                && r.IsRelevant == true
                && r.IsDeleted != true)
            .ToListAsync();

        var tagAnalysisList = new List<TagAccuracyDetail>();

        // Phân tích từng tag
        foreach (var tag in venueTags)
        {
            var analysis = AnalyzeTag(tag, allReviews, goodThreshold, warningThreshold, minReviews);
            if (analysis != null) // Chỉ thêm nếu không null (bỏ qua khoảng giữa)
            {
                tagAnalysisList.Add(analysis);
            }
        }

        // Phân tích tag phổ biến nhất từ phần sau | (tags khách hàng đánh giá venue có)
        var totalReviewsWithSnapshot = allReviews.Count(r => !string.IsNullOrEmpty(r.CoupleMoodSnapshot));
        
        SuggestedTag? mostPopularTag = null;
        
        if (totalReviewsWithSnapshot > 0)
        {
            // Lấy tất cả tags từ phần sau | của tất cả reviews
            var allTagsAfterPipe = allReviews
                .Where(r => !string.IsNullOrEmpty(r.CoupleMoodSnapshot))
                .SelectMany(r =>
                {
                    // Lấy phần sau dấu |
                    var afterPipe = r.CoupleMoodSnapshot.Contains('|')
                        ? r.CoupleMoodSnapshot.Split('|')[1].Trim()
                        : string.Empty;

                    if (string.IsNullOrEmpty(afterPipe))
                        return new List<string>();

                    // Tách thành các tags riêng lẻ
                    return afterPipe
                        .Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(t => t.Trim())
                        .Where(t => !string.IsNullOrEmpty(t))
                        .ToList();
                })
                .ToList();

            // Đếm tần suất xuất hiện của từng tag
            if (allTagsAfterPipe.Any())
            {
                var tagFrequency = allTagsAfterPipe
                    .GroupBy(t => t, StringComparer.OrdinalIgnoreCase)
                    .Select(g => new
                    {
                        Tag = g.Key,
                        Count = g.Count(),
                        Rate = (decimal)g.Count() / totalReviewsWithSnapshot * 100
                    })
                    .Where(x => x.Count >= minReviews && x.Rate >= goodThreshold) // Đủ số lượng và tần suất
                    .OrderByDescending(x => x.Rate)
                    .ToList();

                // Lấy tag phổ biến nhất (nếu có)
                if (tagFrequency.Any())
                {
                    var topTag = tagFrequency.First();
                    
                    mostPopularTag = new SuggestedTag
                    {
                        TagName = topTag.Tag,
                        Count = topTag.Count,
                        MatchRate = Math.Round(topTag.Rate, 1),
                        Message = $"💡 {topTag.Count}/{totalReviewsWithSnapshot} khách hàng ({topTag.Rate:F1}%) đánh giá venue có tag '{topTag.Tag}'. Hãy thêm tag này để được đề xuất nhiều hơn!"
                    };
                }
            }
        }

        // Tạo summary
        var summary = CreateSummary(tagAnalysisList);

        // Cập nhật IsPenalty cho venue (chỉ áp dụng cho MODE 2 - Auto Recommendation)
        var isPenalty = tagAnalysisList.Any(t => t.Status == VenueTagAnalysisConstants.STATUS_POOR);
        
        // Đảm bảo venue được track bởi context
        if (_unitOfWork.Context.Entry(venue).State == EntityState.Detached)
        {
            _unitOfWork.Context.Attach(venue);
        }
        
        venue.IsPenalty = isPenalty;
        _unitOfWork.Context.Entry(venue).State = EntityState.Modified;
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("[TAG ANALYSIS] Venue {VenueId} IsPenalty updated to: {IsPenalty}", 
            venueId, isPenalty);

        // Sync venue lên cả 2 Meilisearch servers (V1 và V2)
        try
        {
            _logger.LogInformation("[TAG ANALYSIS] Syncing venue {VenueId} to Meilisearch V1...", venueId);
            await _meilisearchService.IndexVenueLocationAsync(venueId);
            _logger.LogInformation("[TAG ANALYSIS] Synced venue {VenueId} to Meilisearch V1 successfully", venueId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[TAG ANALYSIS] Failed to sync venue {VenueId} to Meilisearch V1", venueId);
            // Don't throw - continue to sync V2
        }

        try
        {
            _logger.LogInformation("[TAG ANALYSIS] Syncing venue {VenueId} to Meilisearch V2...", venueId);
            await _meilisearchService.IndexVenueLocationV2Async(venueId);
            _logger.LogInformation("[TAG ANALYSIS] Synced venue {VenueId} to Meilisearch V2 successfully", venueId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[TAG ANALYSIS] Failed to sync venue {VenueId} to Meilisearch V2", venueId);
            // Don't throw - analysis still succeeded
        }

        return new VenueTagAnalysisResponse
        {
            VenueId = venueId,
            VenueName = venue.Name,
            TotalReviews = allReviews.Count,
            TagAnalysis = tagAnalysisList,
            Summary = summary,
            MostPopularTag = mostPopularTag
        };
    }

    /// <summary>
    /// Phân tích một tag cụ thể
    /// </summary>
    private TagAccuracyDetail? AnalyzeTag(
        VenueTagInfo tag, 
        List<Data.Entities.Review> allReviews,
        decimal goodThreshold,
        decimal warningThreshold,
        int minReviews)
    {
        // CHỈ phân tích CoupleMoodType (bỏ qua CouplePersonalityType)
        if (tag.Type != "CoupleMoodType")
        {
            return null;
        }

        // Lấy tất cả reviews có snapshot (đã được đánh giá)
        var reviewsWithSnapshot = allReviews
            .Where(r => !string.IsNullOrEmpty(r.CoupleMoodSnapshot))
            .ToList();

        var totalReviews = reviewsWithSnapshot.Count;
        
        // Đếm số lần tag này xuất hiện trong snapshot (= unmatched)
        var unmatchedCount = reviewsWithSnapshot
            .Count(r => 
            {
                // Lấy phần trước dấu | (nếu có)
                var snapshotPart = r.CoupleMoodSnapshot.Contains('|') 
                    ? r.CoupleMoodSnapshot.Split('|')[0].Trim()
                    : r.CoupleMoodSnapshot;
                
                // Bỏ qua nếu là "null" hoặc rỗng
                if (string.IsNullOrEmpty(snapshotPart) || snapshotPart.Equals("null", StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
                
                var tags = snapshotPart
                    .Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(t => t.Trim())
                    .ToList();
                
                return tags.Any(t => t.Equals(tag.Name, StringComparison.OrdinalIgnoreCase));
            });
        
        // Số reviews không có tag này trong snapshot = matched
        var matchedCount = totalReviews - unmatchedCount;

        // Lưu thông tin reviews để log
        var reviewDetails = reviewsWithSnapshot.Select(r => 
        {
            // Lấy phần trước dấu | (nếu có)
            var snapshotPart = r.CoupleMoodSnapshot.Contains('|') 
                ? r.CoupleMoodSnapshot.Split('|')[0].Trim()
                : r.CoupleMoodSnapshot;
            
            // Bỏ qua nếu là "null" hoặc rỗng
            bool hasTag = false;
            if (!string.IsNullOrEmpty(snapshotPart) && !snapshotPart.Equals("null", StringComparison.OrdinalIgnoreCase))
            {
                hasTag = snapshotPart
                    .Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(t => t.Trim())
                    .Any(t => t.Equals(tag.Name, StringComparison.OrdinalIgnoreCase));
            }
            
            return new
            {
                r.Id,
                r.CoupleMoodSnapshot,
                IsMatched = !hasTag // Không có tag trong snapshot = matched
            };
        }).ToList();

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
                Recommendation = null,
                ReviewDetails = reviewDetails.Select(r => $"#{r.Id}: {r.CoupleMoodSnapshot} → {(r.IsMatched ? "✅" : "❌")}").ToList()
            };
        }

        // Tính match rate
        var matchRate = totalReviews > 0 ? (decimal)matchedCount / totalReviews * 100 : 0;

        // Xác định status và severity (CHỈ 2 MỨC: GOOD hoặc POOR, bỏ khoảng giữa)
        string status, severity, recommendation;
        if (matchRate >= goodThreshold)
        {
            // >= 70% → GOOD
            status = VenueTagAnalysisConstants.STATUS_GOOD;
            severity = VenueTagAnalysisConstants.SEVERITY_NONE;
            recommendation = VenueTagAnalysisConstants.RECOMMENDATION_KEEP;
        }
        else if (matchRate < warningThreshold)
        {
            // < 50% → POOR
            status = VenueTagAnalysisConstants.STATUS_POOR;
            severity = VenueTagAnalysisConstants.SEVERITY_HIGH;
            recommendation = VenueTagAnalysisConstants.RECOMMENDATION_REMOVE;
        }
        else
        {
            // 50-69% → Không xử lý (trả về null)
            return null;
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
            Recommendation = recommendation,
            ReviewDetails = reviewDetails.Select(r => $"#{r.Id}: {r.CoupleMoodSnapshot} → {(r.IsMatched ? "✅" : "❌")}").ToList()
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
