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
            var existing = await _unitOfWork.Context.LocationFollowers
                .Where(x => x.OwnerUserId == currentUserId && x.FollowerUserId == targetUserId)
                .FirstOrDefaultAsync();

            if (existing != null)
            {
                // Cập nhật lại status = ACTIVE nếu đã tồn tại
                existing.Status = "ACTIVE";
                existing.UpdatedAt = DateTime.UtcNow;
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
                var follower = new LocationFollower
                {
                    OwnerUserId = currentUserId,
                    FollowerUserId = targetUserId,
                    Status = "ACTIVE",
                    OwnerShareStatus = "SHARING",
                    FollowerShareStatus = "RECEIVING",
                    IsMuted = false,
                    FollowerDisplayName = targetUser.DisplayName,
                    FollowerAvatarUrl = targetUser.AvatarUrl,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _unitOfWork.Context.LocationFollowers.Add(follower);
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
            var follower = await _unitOfWork.Context.LocationFollowers
                .Where(x => x.OwnerUserId == currentUserId && x.FollowerUserId == targetUserId)
                .FirstOrDefaultAsync();

            if (follower != null)
            {
                follower.Status = "REMOVED";
                follower.UpdatedAt = DateTime.UtcNow;
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
            var followers = await _unitOfWork.Context.LocationFollowers
                .Where(x => x.OwnerUserId == currentUserId && x.Status == "ACTIVE")
                .ToListAsync();

            return followers.Select(f => new LocationFollowerDto
            {
                UserId = f.FollowerUserId,
                DisplayName = f.FollowerDisplayName,
                AvatarUrl = f.FollowerAvatarUrl,
                Status = f.Status,
                LastSeenAt = f.LastSeenAt
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
            var followers = await _unitOfWork.Context.LocationFollowers
                .Where(x => x.FollowerUserId == currentUserId && x.Status == "ACTIVE")
                .ToListAsync();

            return followers.Select(f => new LocationFollowerDto
            {
                UserId = f.OwnerUserId,
                DisplayName = f.OwnerDisplayName,
                AvatarUrl = f.OwnerAvatarUrl,
                Status = f.Status,
                LastSeenAt = f.LastSeenAt
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting followers");
            return new List<LocationFollowerDto>();
        }
    }
}
