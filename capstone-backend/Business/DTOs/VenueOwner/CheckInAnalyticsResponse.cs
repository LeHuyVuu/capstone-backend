namespace capstone_backend.Business.DTOs.VenueOwner;

/// <summary>
/// Response chi tiết phân tích check-ins
/// </summary>
public class CheckInAnalyticsResponse
{
    public int TotalCheckIns { get; set; }
    public int UniqueCustomers { get; set; }
    public decimal AverageCheckInsPerCustomer { get; set; }
    
    // Check-ins theo venue
    public List<VenueCheckInStats> VenueStats { get; set; } = new();
    
    // Peak hours (giờ cao điểm)
    public List<HourlyPattern> PeakHours { get; set; } = new();
    
    // Check-ins theo ngày trong tuần
    public List<DailyPattern> DailyPatterns { get; set; } = new();
    
    // Conversion rate
    public ConversionMetrics Conversion { get; set; } = new();
}

public class VenueCheckInStats
{
    public int VenueId { get; set; }
    public string VenueName { get; set; } = string.Empty;
    public int CheckInCount { get; set; }
    public int UniqueCustomers { get; set; }
    public int ValidCheckIns { get; set; }
    public int InvalidCheckIns { get; set; }
}

public class HourlyPattern
{
    public int Hour { get; set; }
    public string TimeRange { get; set; } = string.Empty; // "08:00-09:00"
    public int CheckInCount { get; set; }
}

public class DailyPattern
{
    public string DayOfWeek { get; set; } = string.Empty;
    public int CheckInCount { get; set; }
    public int UniqueCustomers { get; set; }
}

public class ConversionMetrics
{
    public int TotalCheckIns { get; set; }
    public int CheckInsWithReview { get; set; }
    public decimal ConversionRate { get; set; }
}
