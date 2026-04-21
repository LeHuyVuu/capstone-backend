using capstone_backend.Business.DTOs.Accessory;

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
    public int? Age { get; set; }
    public string? PersonalityResultCode { get; set; }
    public List<string>? PersonalityDescription { get; set; }
    public string? Bio { get; set; }
    public string? RelationshipStatus { get; set; }
    public string? JobTitle { get; set; }
    public string? EducationLevel { get; set; }
    public int? Height { get; set; }
    public int? Weight { get; set; }
    public string? City { get; set; }
    public string? District { get; set; }
    public decimal? HomeLatitude { get; set; }
    public decimal? HomeLongitude { get; set; }
    public decimal? BudgetMin { get; set; }
    public decimal? BudgetMax { get; set; }
    public object? FavoritePets { get; set; }
    public bool? HasPet { get; set; }
    public bool? Smoking { get; set; }
    public object? Interests { get; set; }
    public object? AvailableTime { get; set; }
    public string? InviteCode { get; set; }
    public string? PhoneNumber { get; set; }
    public string? AvatarUrl { get; set; }
    public string? Address { get; set; }
    public string? Area { get; set; }

    public List<EquippedAccessoryBriefResponse> EquippedAccessories { get; set; } = new List<EquippedAccessoryBriefResponse>();
}
