using capstone_backend.Business.DTOs.LocationTracking;

namespace capstone_backend.Business.Interfaces;

/// <summary>
/// Service đơn giản để quản lý watchlist (danh sách theo dõi vị trí)
/// </summary>
public interface ILocationFollowerService
{
    /// <summary>
    /// Thêm user vào watchlist của mình
    /// </summary>
    Task<bool> AddToWatchlistAsync(int currentUserId, long targetUserId);

    /// <summary>
    /// Xóa user khỏi watchlist
    /// </summary>
    Task<bool> RemoveFromWatchlistAsync(int currentUserId, long targetUserId);

    /// <summary>
    /// Lấy danh sách người mình đang theo dõi
    /// </summary>
    Task<List<LocationFollowerDto>> GetMyWatchlistAsync(int currentUserId);

    /// <summary>
    /// Lấy danh sách người đang theo dõi mình
    /// </summary>
    Task<List<LocationFollowerDto>> GetMyFollowersAsync(int currentUserId);
}
