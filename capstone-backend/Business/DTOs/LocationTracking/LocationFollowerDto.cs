namespace capstone_backend.Business.DTOs.LocationTracking;

/// <summary>
/// DTO cho thông tin người theo dõi hoặc được theo dõi
/// </summary>
public class LocationFollowerDto
{
    public long UserId { get; set; }
    public string? DisplayName { get; set; }
    public string? AvatarUrl { get; set; }
    public string Status { get; set; } = "ACTIVE"; // ACTIVE, REMOVED, BLOCKED
    public DateTime? LastSeenAt { get; set; }
}

/// <summary>
/// Request để thêm/xóa người vào watchlist
/// </summary>
public class WatchlistRequest
{
    public long TargetUserId { get; set; }
}
