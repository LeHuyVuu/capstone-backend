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
