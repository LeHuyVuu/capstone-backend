namespace capstone_backend.Business.DTOs.CoupleInvitation;

/// <summary>
/// Response DTO cho member profile (dùng khi search hoặc get by invite code)
/// </summary>
public class MemberProfileResponse
{
    public int MemberProfileId { get; set; }
    public int UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public string? PhoneNumber { get; set; }

    public DateOnly? DateOfBirth { get; set; }
    public string? Gender { get; set; }
    public string? Bio { get; set; }
    public string? RelationshipStatus { get; set; }
    public string? JobTitle { get; set; }
    public string? EducationLevel { get; set; }
    public double? Height { get; set; }
    public double? Weight { get; set; }

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

    public string? Address { get; set; }
    public string? Area { get; set; }

    public string? InviteCode { get; set; }
    public bool CanSendInvitation { get; set; }
    public int? Age { get; set; }
}
