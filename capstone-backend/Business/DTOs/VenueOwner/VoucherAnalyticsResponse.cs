namespace capstone_backend.Business.DTOs.VenueOwner;

/// <summary>
/// Response chi tiết phân tích vouchers
/// </summary>
public class VoucherAnalyticsResponse
{
    public int TotalVouchers { get; set; }
    public int ActiveVouchers { get; set; }
    public int ExpiredVouchers { get; set; }
    
    // Vouchers theo status
    public VoucherStatusBreakdown StatusBreakdown { get; set; } = new();
    
    // Performance metrics
    public VoucherPerformanceMetrics Performance { get; set; } = new();
    
    // Top vouchers
    public List<TopVoucher> TopVouchers { get; set; } = new();
    
    // Vouchers sắp hết hạn
    public List<ExpiringVoucher> ExpiringVouchers { get; set; } = new();
}

public class VoucherStatusBreakdown
{
    public int Pending { get; set; }
    public int Approved { get; set; }
    public int Rejected { get; set; }
    public int Active { get; set; }
    public int Expired { get; set; }
}

public class VoucherPerformanceMetrics
{
    public int TotalQuantity { get; set; }
    public int TotalExchanged { get; set; }
    public int TotalUsed { get; set; }
    public decimal ExchangeRate { get; set; }
    public decimal UsageRate { get; set; }
    public int TotalPointsEarned { get; set; }
}

public class TopVoucher
{
    public int VoucherId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public int ExchangedCount { get; set; }
    public int UsedCount { get; set; }
    public decimal UsageRate { get; set; }
    public int PointPrice { get; set; }
}

public class ExpiringVoucher
{
    public int VoucherId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public DateTime? EndDate { get; set; }
    public int DaysUntilExpiry { get; set; }
    public int RemainingQuantity { get; set; }
}
