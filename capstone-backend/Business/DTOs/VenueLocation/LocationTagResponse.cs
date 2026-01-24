namespace capstone_backend.Business.DTOs.VenueLocation;

/// <summary>
/// Response model for location tag
/// </summary>
public class LocationTagResponse
{
    public int Id { get; set; }
    public string? TagName { get; set; }
    public CoupleMoodTypeInfo? CoupleMoodType { get; set; }
    public CouplePersonalityTypeInfo? CouplePersonalityType { get; set; }
}
