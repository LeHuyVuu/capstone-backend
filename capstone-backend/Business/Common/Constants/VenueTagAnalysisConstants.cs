namespace capstone_backend.Business.Common.Constants;

/// <summary>
/// Constants cho venue tag analysis system
/// Các giá trị này được lưu trong SystemConfig và có thể điều chỉnh bởi admin
/// </summary>
public static class VenueTagAnalysisConstants
{
    // System Config Keys
    public const string GOOD_THRESHOLD_KEY = "VENUE_TAG_GOOD_THRESHOLD";
    public const string WARNING_THRESHOLD_KEY = "VENUE_TAG_WARNING_THRESHOLD";
    public const string MIN_REVIEWS_KEY = "VENUE_TAG_MIN_REVIEWS";
    
    // Default values (nếu chưa có trong SystemConfig)
    public const decimal DEFAULT_GOOD_THRESHOLD = 70m; // >= 70% = GOOD
    public const decimal DEFAULT_WARNING_THRESHOLD = 50m; // 50-69% = WARNING, < 50% = POOR
    public const int DEFAULT_MIN_REVIEWS = 3; // Cần ít nhất 3 reviews để đánh giá
    
    // Status values
    public const string STATUS_GOOD = "GOOD";
    public const string STATUS_WARNING = "WARNING";
    public const string STATUS_POOR = "POOR";
    public const string STATUS_INSUFFICIENT_DATA = "INSUFFICIENT_DATA";
    
    // Severity values
    public const string SEVERITY_NONE = "NONE";
    public const string SEVERITY_MEDIUM = "MEDIUM";
    public const string SEVERITY_HIGH = "HIGH";
    
    // Recommendation values
    public const string RECOMMENDATION_KEEP = "KEEP_TAG";
    public const string RECOMMENDATION_REVIEW = "REVIEW_TAG";
    public const string RECOMMENDATION_REMOVE = "REMOVE_TAG";
}
