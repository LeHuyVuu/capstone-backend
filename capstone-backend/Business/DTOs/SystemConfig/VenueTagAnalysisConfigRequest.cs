using System.ComponentModel.DataAnnotations;

namespace capstone_backend.Business.DTOs.SystemConfig;

/// <summary>
/// Request để admin cập nhật các ngưỡng cho venue tag analysis
/// </summary>
public class UpdateVenueTagAnalysisConfigRequest
{
    /// <summary>
    /// Ngưỡng GOOD (>= giá trị này = GOOD)
    /// </summary>
    [Range(0, 100)]
    public decimal? GoodThreshold { get; set; }

    /// <summary>
    /// Ngưỡng WARNING (>= giá trị này và < GoodThreshold = WARNING)
    /// </summary>
    [Range(0, 100)]
    public decimal? WarningThreshold { get; set; }

    /// <summary>
    /// Số reviews tối thiểu để đánh giá tag
    /// </summary>
    [Range(1, 100)]
    public int? MinReviews { get; set; }
}
