namespace capstone_backend.Business.DTOs.VenueLocation;

/// <summary>
/// Venue opening hour information for a specific day
/// </summary>
public class VenueOpeningHourResponse
{
    public int Id { get; set; }
    public int Day { get; set; }
    public TimeSpan OpenTime { get; set; }
    public TimeSpan CloseTime { get; set; }
    public bool IsClosed { get; set; }
}
