namespace capstone_backend.Business.DTOs.VenueLocation;

/// <summary>
/// Response for venue opening hour information
/// </summary>
public class VenueOpeningHourResponse
{
    /// <summary>
    /// Opening hour record ID
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Venue location ID
    /// </summary>
    public int VenueLocationId { get; set; }

    /// <summary>
    /// Day of week (2-8, where 2=Monday, 3=Tuesday, ..., 8=Sunday)
    /// </summary>
    public int Day { get; set; }

    /// <summary>
    /// Opening time
    /// </summary>
    public TimeSpan OpenTime { get; set; }

    /// <summary>
    /// Closing time
    /// </summary>
    public TimeSpan CloseTime { get; set; }

    /// <summary>
    /// Whether the venue is closed on this day
    /// </summary>
    public bool IsClosed { get; set; }
}
