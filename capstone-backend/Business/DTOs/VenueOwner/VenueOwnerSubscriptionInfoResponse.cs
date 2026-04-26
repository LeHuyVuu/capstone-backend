namespace capstone_backend.Business.DTOs.VenueOwner;

/// <summary>
/// Response DTO cho thông tin subscription của venue owner
/// </summary>
public class VenueOwnerSubscriptionInfoResponse
{
    public bool HasActiveSubscription { get; set; }
    
    /// <summary>
    /// Danh sách các gói subscription đang active
    /// </summary>
    public List<ActiveSubscriptionDetail> ActiveSubscriptions { get; set; } = new();
    
    /// <summary>
    /// Thông tin tính năng VENUE_INSIGHT
    /// </summary>
    public FeatureAccessInfo? VenueInsightAccess { get; set; }
}

public class ActiveSubscriptionDetail
{
    public int SubscriptionId { get; set; }
    public int PackageId { get; set; }
    public string? PackageName { get; set; }
    public string? PackageType { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int? VenueId { get; set; }
    public Dictionary<string, bool>? Features { get; set; }
}

public class FeatureAccessInfo
{
    public bool HasAccess { get; set; }
    
    /// <summary>
    /// Ngày hết hạn xa nhất của tính năng này (từ tất cả các gói)
    /// </summary>
    public DateTime? ExpiryDate { get; set; }
    
    /// <summary>
    /// Số ngày còn lại
    /// </summary>
    public int? DaysRemaining { get; set; }
    
    /// <summary>
    /// Danh sách các gói đang cung cấp tính năng này
    /// </summary>
    public List<string> ProvidingPackages { get; set; } = new();
}
