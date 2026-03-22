namespace capstone_backend.Business.DTOs.VenueLocation;

/// <summary>
/// Request to update venue opening hours for all days of the week
/// </summary>
public class UpdateVenueOpeningHoursRequest
{
    public int VenueLocationId { get; set; }
    public List<VenueOpeningHourDto> OpeningHours { get; set; } = new();
}

/// <summary>
/// Opening hour information for a specific day
/// </summary>
public class VenueOpeningHourDto
{
    /// <summary>
    /// Day of week: 2=Monday, 3=Tuesday, 4=Wednesday, 5=Thursday, 6=Friday, 7=Saturday, 8=Sunday
    /// </summary>
    public int Day { get; set; }
    
    /// <summary>
    /// Opening time in HH:mm format (e.g., "08:00")
    /// </summary>
    public string? OpenTime { get; set; }
    
    /// <summary>
    /// Closing time in HH:mm format (e.g., "22:00")
    /// </summary>
    public string? CloseTime { get; set; }
    
    /// <summary>
    /// Whether the venue is closed on this day
    /// </summary>
    public bool IsClosed { get; set; }
}
