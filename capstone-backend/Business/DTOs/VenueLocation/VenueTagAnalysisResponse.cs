namespace capstone_backend.Business.DTOs.VenueLocation;

/// <summary>
/// Response DTO cho phân tích độ chính xác của tags
/// </summary>
public class VenueTagAnalysisResponse
{
    public int VenueId { get; set; }
    public string VenueName { get; set; } = string.Empty;
    public decimal OverallMatchRate { get; set; }
    public int TotalReviews { get; set; }
    public List<TagAccuracyDetail> TagAnalysis { get; set; } = new();
    public TagAnalysisSummary Summary { get; set; } = new();
}

/// <summary>
/// Chi tiết độ chính xác của từng tag
/// </summary>
public class TagAccuracyDetail
{
    public string Tag { get; set; } = string.Empty;
    public string TagType { get; set; } = string.Empty; // CoupleMoodType hoặc CouplePersonalityType
    public string Status { get; set; } = string.Empty; // GOOD, WARNING, POOR, INSUFFICIENT_DATA
    public string Severity { get; set; } = string.Empty; // NONE, MEDIUM, HIGH
    public int TotalReviews { get; set; }
    public int MatchedCount { get; set; }
    public int UnmatchedCount { get; set; }
    public decimal MatchRate { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? Recommendation { get; set; }
}

/// <summary>
/// Tóm tắt phân tích tags
/// </summary>
public class TagAnalysisSummary
{
    public List<string> GoodTags { get; set; } = new();
    public List<string> WarningTags { get; set; } = new();
    public List<string> PoorTags { get; set; } = new();
    public bool ActionRequired { get; set; }
    public string OverallMessage { get; set; } = string.Empty;
    public string? ImpactMessage { get; set; }
}
