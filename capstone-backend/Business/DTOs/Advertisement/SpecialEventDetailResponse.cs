namespace capstone_backend.Business.DTOs.Advertisement;

/// <summary>
/// Special event detail response
/// Used when user clicks on a special event banner
/// </summary>
public class SpecialEventDetailResponse
{
    public int Id { get; set; }
    public string EventName { get; set; } = null!;
    public string? Description { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? BannerUrl { get; set; }
    public bool? IsYearly { get; set; }
    public DateTime? CreatedAt { get; set; }
}
