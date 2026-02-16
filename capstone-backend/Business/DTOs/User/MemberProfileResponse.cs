namespace capstone_backend.Business.DTOs.User;

/// <summary>
/// Response model for member profile data
/// </summary>
public class MemberProfileResponse
{
    public int Id { get; set; }
    public string? FullName { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public string? Gender { get; set; }
    public string? Bio { get; set; }
    public string? RelationshipStatus { get; set; }
    public decimal? HomeLatitude { get; set; }
    public decimal? HomeLongitude { get; set; }
    public decimal? BudgetMin { get; set; }
    public decimal? BudgetMax { get; set; }
    public object? Interests { get; set; }
    public object? AvailableTime { get; set; }
    public string? InviteCode { get; set; }
}
