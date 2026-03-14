namespace capstone_backend.Business.DTOs.Admin;

public class AdminDashboardResponse
{
    public int TotalUsers { get; set; }
    public int TotalVenueOwnerProfiles { get; set; }
    public int TotalVenueLocations { get; set; }
    public int TotalMemberProfiles { get; set; }
    public int ActiveCouples { get; set; }
    public int TotalTransactions { get; set; }
    public decimal TotalRevenue { get; set; }
    public int TotalReports { get; set; }
    public int TotalPosts { get; set; }
    public int TotalAdsOrders { get; set; }
    public int ActiveAdsOrders { get; set; }
    public int TotalMemberSubscriptions { get; set; }
    public int ActiveMemberSubscriptions { get; set; }
    public int TotalVenueSubscriptions { get; set; }
    public int ActiveVenueSubscriptions { get; set; }
    
    public List<ChartDataPoint> UserGrowthChart { get; set; } = new();
    public List<ChartDataPoint> RevenueChart { get; set; } = new();
    public List<ChartDataPoint> TransactionChart { get; set; } = new();
    public List<ChartDataPoint> VenueGrowthChart { get; set; } = new();
    public List<ChartDataPoint> PostActivityChart { get; set; } = new();
}

public class ChartDataPoint
{
    public string Label { get; set; } = null!;
    public decimal Value { get; set; }
}
