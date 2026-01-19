namespace capstone_backend.Business.DTOs.Member;

/// <summary>
/// Response model for couple profile data
/// </summary>
public class CoupleProfileResponse
{
    public int Id { get; set; }
    public int MemberId1 { get; set; }
    public int MemberId2 { get; set; }
    public string? CoupleName { get; set; }
    public DateOnly? StartDate { get; set; }
    public DateOnly? AniversaryDate { get; set; }
    public string? Status { get; set; }
    public DateTime CreatedAt { get; set; }
    
    // Optional: Include member names for display
    public string? Member1Name { get; set; }
    public string? Member2Name { get; set; }
}
