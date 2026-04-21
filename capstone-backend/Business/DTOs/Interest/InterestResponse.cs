namespace capstone_backend.Business.DTOs.Interest;

/// <summary>
/// Response DTO for interest/hobby
/// </summary>
public class InterestResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
}
