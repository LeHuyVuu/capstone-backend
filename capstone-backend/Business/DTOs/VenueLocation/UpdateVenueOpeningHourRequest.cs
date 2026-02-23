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
    /// Supports overnight hours (e.g., "23:00" open, "02:00" close)
    /// </summary>
    public string OpenTime { get; set; } = null!;

    /// <summary>
    /// Closing time in format HH:mm (e.g., "23:00")
    /// Can be less than OpenTime for overnight venues (e.g., "02:00")
    /// </summary>
    public string CloseTime { get; set; } = null!;

    /// <summary>
    /// Manual override: Set to true to temporarily close venue during opening hours
    /// If not provided or false, IsClosed will be calculated automatically based on current time
    /// </summary>
    public bool? IsClosed { get; set; }
}
