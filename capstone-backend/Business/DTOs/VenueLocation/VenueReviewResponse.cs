namespace capstone_backend.Business.DTOs.VenueLocation;

/// <summary>
/// Response model for venue location review
/// </summary>
public class VenueReviewResponse
{
    public int Id { get; set; }
    public int VenueId { get; set; }
    public int? Rating { get; set; }
    public string? Content { get; set; }
    public DateTime? VisitedAt { get; set; }
    public bool? IsAnonymous { get; set; }
    public int? LikeCount { get; set; }
    public string? Status { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Member information (người viết review)
    public ReviewMemberInfo? Member { get; set; }

    /// <summary>
    /// Danh sách ảnh đính kèm trong review
    /// </summary>
    public List<string>? ImageUrls { get; set; }

    /// <summary>
    /// Tag matched hoặc not matched bằng tiếng Việt
    /// </summary>
    public string? MatchedTag { get; set; }

    /// <summary>
    /// Danh sách likes của review
    /// </summary>
    public List<ReviewLikeInfo>? ReviewLikes { get; set; }
}

/// <summary>
/// Review like information
/// </summary>
public class ReviewLikeInfo
{
    public int Id { get; set; }
    public int? MemberId { get; set; }
    public DateTime? CreatedAt { get; set; }
    
    // Member information (người like review)
    public ReviewMemberInfo? Member { get; set; }
}

/// <summary>
/// Member information for review
/// </summary>
public class ReviewMemberInfo
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string? FullName { get; set; }
    public string? Gender { get; set; }
    public string? Bio { get; set; }
    
    // User account info
    public string? DisplayName { get; set; }
    public string? AvatarUrl { get; set; }
    public string? Email { get; set; }
}

/// <summary>
/// Tóm tắt đánh giá cho venue
/// </summary>
public class ReviewSummary
{
    /// <summary>
    /// Rating trung bình
    /// </summary>
    public decimal AverageRating { get; set; }

    /// <summary>
    /// Tổng số reviews
    /// </summary>
    public int TotalReviews { get; set; }

    /// <summary>
    /// Phân bố rating theo số sao
    /// </summary>
    public List<RatingDistribution> Ratings { get; set; } = new();

    /// <summary>
    /// Tỉ lệ phần trăm reviews phù hợp với mood của venue
    /// </summary>
    public decimal MoodMatchPercentage { get; set; }

    /// <summary>
    /// Số lượng reviews phù hợp với mood
    /// </summary>
    public int MatchedReviewsCount { get; set; }
}

/// <summary>
/// Phân bố rating theo số sao
/// </summary>
public class RatingDistribution
{
    /// <summary>
    /// Số sao (1-5)
    /// </summary>
    public int Star { get; set; }

    /// <summary>
    /// Số lượng reviews có rating này
    /// </summary>
    public int Count { get; set; }

    /// <summary>
    /// Phần trăm reviews có rating này
    /// </summary>
    public decimal Percent { get; set; }
}
