namespace capstone_backend.Business.DTOs.Member;

public class UpdateMemberProfileRequest
{
    public string? FullName { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public string? Gender { get; set; }
    public string? Bio { get; set; }
    public int? Age { get; set; }
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
    public List<string>? Interests { get; set; }
    public List<string>? FavoritePets { get; set; }
    public bool? HasPet { get; set; }
    public bool? Smoking { get; set; }
    public string? Address { get; set; }
    public string? Area { get; set; }
    public string? AvatarUrl { get; set; }
    public string? PhoneNumber { get; set; }
}
