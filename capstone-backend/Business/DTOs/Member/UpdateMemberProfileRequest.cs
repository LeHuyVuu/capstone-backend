namespace capstone_backend.Business.DTOs.Member;

public class UpdateMemberProfileRequest
{
    public string? FullName { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public string? Gender { get; set; }
    public string? Bio { get; set; }
    public decimal? HomeLatitude { get; set; }
    public decimal? HomeLongitude { get; set; }
    public decimal? BudgetMin { get; set; }
    public decimal? BudgetMax { get; set; }
    public string? Address { get; set; }
    public string? Area { get; set; }
    public string? AvatarUrl { get; set; }
    public string? PhoneNumber { get; set; }
}
