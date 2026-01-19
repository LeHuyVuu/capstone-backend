using capstone_backend.Business.DTOs.LocationTracking;
using capstone_backend.Business.Interfaces;
using capstone_backend.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace capstone_backend.Business.Services;

/// <summary>
/// Service đơn giản quản lý watchlist - chỉ dùng PostgreSQL, không cần Firebase
/// Firebase Realtime Database được Flutter app tự xử lý trực tiếp
/// </summary>
public class LocationFollowerService : ILocationFollowerService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<LocationFollowerService> _logger;

    public LocationFollowerService(IUnitOfWork unitOfWork, ILogger<LocationFollowerService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<bool> AddToWatchlistAsync(int currentUserId, long targetUserId)
    {
        try
        {
            // Kiểm tra đã tồn tại chưa
            var existing = await _unitOfWork.Context.location_followers
                .Where(x => x.owner_user_id == currentUserId && x.follower_user_id == targetUserId)
                .FirstOrDefaultAsync();

            if (existing != null)
            {
                // Cập nhật lại status = ACTIVE nếu đã tồn tại
                existing.status = "ACTIVE";
                existing.updated_at = DateTime.UtcNow;
            }
            else
            {
                // Lấy thông tin target user
                var targetUser = await _unitOfWork.Users.GetByIdAsync((int)targetUserId);
                if (targetUser == null)
                {
                    _logger.LogWarning("Target user {TargetUserId} not found", targetUserId);
                    return false;
                }

                // Tạo mới
                var follower = new location_follower
                {
                    owner_user_id = currentUserId,
                    follower_user_id = targetUserId,
                    status = "ACTIVE",
                    owner_share_status = "SHARING",
                    follower_share_status = "RECEIVING",
                    is_muted = false,
                    follower_display_name = targetUser.display_name,
                    follower_avatar_url = targetUser.avatar_url,
                    created_at = DateTime.UtcNow,
                    updated_at = DateTime.UtcNow
                };

                _unitOfWork.Context.location_followers.Add(follower);
            }

            await _unitOfWork.SaveChangesAsync();
            _logger.LogInformation("User {CurrentUserId} added {TargetUserId} to watchlist", currentUserId, targetUserId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding to watchlist");
            return false;
        }
    }

    public async Task<bool> RemoveFromWatchlistAsync(int currentUserId, long targetUserId)
    {
        try
        {
            var follower = await _unitOfWork.Context.location_followers
                .Where(x => x.owner_user_id == currentUserId && x.follower_user_id == targetUserId)
                .FirstOrDefaultAsync();

            if (follower != null)
            {
                follower.status = "REMOVED";
                follower.updated_at = DateTime.UtcNow;
                await _unitOfWork.SaveChangesAsync();
            }

            _logger.LogInformation("User {CurrentUserId} removed {TargetUserId} from watchlist", currentUserId, targetUserId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing from watchlist");
            return false;
        }
    }

    public async Task<List<LocationFollowerDto>> GetMyWatchlistAsync(int currentUserId)
    {
        try
        {
            var followers = await _unitOfWork.Context.location_followers
                .Where(x => x.owner_user_id == currentUserId && x.status == "ACTIVE")
                .ToListAsync();

            return followers.Select(f => new LocationFollowerDto
            {
                UserId = f.follower_user_id,
                DisplayName = f.follower_display_name,
                AvatarUrl = f.follower_avatar_url,
                Status = f.status,
                LastSeenAt = f.last_seen_at
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting watchlist");
            return new List<LocationFollowerDto>();
        }
    }

    public async Task<List<LocationFollowerDto>> GetMyFollowersAsync(int currentUserId)
    {
        try
        {
            var followers = await _unitOfWork.Context.location_followers
                .Where(x => x.follower_user_id == currentUserId && x.status == "ACTIVE")
                .ToListAsync();

            return followers.Select(f => new LocationFollowerDto
            {
                UserId = f.owner_user_id,
                DisplayName = f.owner_display_name,
                AvatarUrl = f.owner_avatar_url,
                Status = f.status,
                LastSeenAt = f.last_seen_at
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting followers");
            return new List<LocationFollowerDto>();
        }
    }
}
