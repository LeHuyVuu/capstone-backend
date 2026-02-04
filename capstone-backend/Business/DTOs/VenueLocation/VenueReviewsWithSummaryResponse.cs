using capstone_backend.Business.DTOs.Common;

namespace capstone_backend.Business.DTOs.VenueLocation;

/// <summary>
/// Response chứa danh sách reviews và summary cho venue
/// </summary>
public class VenueReviewsWithSummaryResponse
{
    /// <summary>
    /// Tóm tắt đánh giá (rating trung bình, phân bố số sao)
    /// </summary>
    public ReviewSummary Summary { get; set; } = new();

    /// <summary>
    /// Danh sách reviews (có phân trang)
    /// </summary>
    public PagedResult<VenueReviewResponse> Reviews { get; set; } = null!;
}
