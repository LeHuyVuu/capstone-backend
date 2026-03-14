namespace capstone_backend.Business.DTOs.CoupleProfile;

/// <summary>
/// Request DTO để update couple profile
/// </summary>
public class UpdateCoupleProfileRequest
{
    public string? CoupleName { get; set; }
    public DateOnly? StartDate { get; set; }
    public DateOnly? AniversaryDate { get; set; }
    public decimal? BudgetMin { get; set; }
    public decimal? BudgetMax { get; set; }
}
