namespace capstone_backend.Business.DTOs.CoupleProfile;

/// <summary>
/// Response DTO cho chi tiết couple profile
/// </summary>
public class CoupleProfileDetailResponse
{
    public int CoupleId { get; set; }
    public string? CoupleName { get; set; }
    public DateOnly? StartDate { get; set; }
    public DateOnly? AniversaryDate { get; set; }
    public decimal? BudgetMin { get; set; }
    public decimal? BudgetMax { get; set; }
    public int? TotalPoints { get; set; }
    public int? InteractionPoints { get; set; }
    public string? Status { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    
    // Member 1 info
    public int MemberId1 { get; set; }
    public string Member1Name { get; set; } = string.Empty;
    public string? Member1AvatarUrl { get; set; }
    public string? Member1Gender { get; set; }
    public DateOnly? Member1DateOfBirth { get; set; }
    
    // Member 2 info
    public int MemberId2 { get; set; }
    public string Member2Name { get; set; } = string.Empty;
    public string? Member2AvatarUrl { get; set; }
    public string? Member2Gender { get; set; }
    public DateOnly? Member2DateOfBirth { get; set; }
    
    // Personality & Mood
    public int? CouplePersonalityTypeId { get; set; }
    public string? CouplePersonalityTypeName { get; set; }
    public string? CouplePersonalityTypeDescription { get; set; }
    
    public int? CoupleMoodTypeId { get; set; }
    public string? CoupleMoodTypeName { get; set; }
    public string? CoupleMoodTypeDescription { get; set; }
}
