namespace capstone_backend.Business.DTOs.VenueLocation;

/// <summary>
/// Request to update venue opening hours
/// </summary>
public class UpdateVenueOpeningHourRequest
{
    /// <summary>
    /// Venue location ID
    /// </summary>
    public int VenueLocationId { get; set; }

    /// <summary>
    /// Day of week (2-8, where 2=Monday, 3=Tuesday, ..., 8=Sunday)
    /// </summary>
    public int Day { get; set; }

    /// <summary>
    /// Opening time in format HH:mm (e.g., "09:00")
    /// </summary>
    public string OpenTime { get; set; } = null!;

    /// <summary>
    /// Closing time in format HH:mm (e.g., "23:00")
    /// </summary>
    public string CloseTime { get; set; } = null!;

    public Boolean IsClosed { get; set; }
}
