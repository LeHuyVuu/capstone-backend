using capstone_backend.Business.DTOs.VenueLocation;

namespace capstone_backend.Business.Interfaces;

/// <summary>
/// Service interface cho venue tag analysis
/// </summary>
public interface IVenueTagAnalysisService
{
    /// <summary>
    /// Phân tích độ chính xác của tất cả tags của venue
    /// Dựa trên CoupleMoodSnapshot trong reviews
    /// </summary>
    /// <param name="venueId">Venue ID</param>
    /// <returns>Phân tích chi tiết từng tag và tổng quan</returns>
    Task<VenueTagAnalysisResponse> AnalyzeVenueTagsAsync(int venueId);
}
