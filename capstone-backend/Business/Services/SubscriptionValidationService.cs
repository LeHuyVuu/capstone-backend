using capstone_backend.Business.Interfaces;
using capstone_backend.Data.Entities;
using capstone_backend.Data.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace capstone_backend.Business.Services;

/// <summary>
/// Service for validating user subscription packages
/// </summary>
public class SubscriptionValidationService : ISubscriptionValidationService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<SubscriptionValidationService> _logger;
    private readonly capstone_backend.Data.Context.MyDbContext _context;

    public SubscriptionValidationService(
        IUnitOfWork unitOfWork,
        ILogger<SubscriptionValidationService> logger,
        capstone_backend.Data.Context.MyDbContext context)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _context = context;
    }

    /// <summary>
    /// Validate subscription based on user type
    /// </summary>
    public async Task<(bool isActive, string? errorMessage)> ValidateSubscriptionAsync(
        int userId, 
        string userType,
        string? featureCode = null)
    {
        try
        {
            // Normalize userType to uppercase
            var normalizedType = userType?.ToUpper().Trim();

            _logger.LogInformation(
                "Validating subscription for user ID: {UserId}, Type: {UserType}", 
                userId, 
                normalizedType);

            if (string.IsNullOrEmpty(normalizedType))
            {
                _logger.LogWarning("User type is null or empty for user ID: {UserId}", userId);
                return (false, "User type not specified");
            }

            // Route to appropriate validation method based on user type
            if (normalizedType.Contains("MEMBER"))
            {
                return await ValidateMemberSubscriptionAsync(userId, featureCode);
            }
            else if (normalizedType.Contains("VENUEOWNER"))
            {
                return await ValidateVenueOwnerSubscriptionAsync(userId, featureCode);
            }
            else
            {
                _logger.LogWarning("Unknown user type: {UserType} for user ID: {UserId}", userType, userId);
                return (false, $"Unknown user type: {userType}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating subscription for user ID: {UserId}", userId);
            throw;
        }
    }

    /// <summary>
    /// Validate Member subscription
    /// </summary>
    public async Task<(bool isActive, string? errorMessage)> ValidateMemberSubscriptionAsync(int userId, string? featureCode = null)
    {
        try
        {
            _logger.LogInformation("Validating Member subscription for user ID: {UserId}", userId);

            // 1. Find MemberProfile by userId
            var memberProfile = await _context.MemberProfiles
                .FirstOrDefaultAsync(m => 
                    m.UserId == userId && 
                    m.IsDeleted != true);

            if (memberProfile == null)
            {
                _logger.LogWarning("Member profile not found for user ID: {UserId}", userId);
                return (false, "Member profile not found");
            }

            // 2. Check for active subscription
            var now = DateTime.UtcNow;
            var activeSub = await _context.MemberSubscriptionPackages
                .Where(msp => 
                    msp.MemberId == memberProfile.Id &&
                    msp.Status == MemberSubscriptionPackageStatus.ACTIVE.ToString() &&
                    msp.EndDate.HasValue &&
                    msp.EndDate.Value >= now
                )
                .Include(msp => msp.Package)
                .OrderByDescending(msp => msp.EndDate)
                .FirstOrDefaultAsync();

            if (activeSub == null)
            {
                _logger.LogInformation(
                    "No active subscription found for member ID: {MemberId} (User ID: {UserId})", 
                    memberProfile.Id, 
                    userId);
                return (false, "Gói của bạn đã hết hạn để sài tính năng này");
            }

            _logger.LogInformation(
                "Active subscription found for member ID: {MemberId}, Subscription ID: {SubId}, EndDate: {EndDate}", 
                memberProfile.Id, 
                activeSub.Id,
                activeSub.EndDate);

            if (!HasFeatureAccess(activeSub.Package?.FeatureFlags, featureCode))
            {
                _logger.LogInformation(
                    "Feature access denied for member ID: {MemberId}, FeatureCode: {FeatureCode}, PackageId: {PackageId}",
                    memberProfile.Id,
                    featureCode,
                    activeSub.PackageId);
                return (false, "Gói hiện tại không hỗ trợ tính năng này");
            }

            return (true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating Member subscription for user ID: {UserId}", userId);
            throw;
        }
    }

    /// <summary>
    /// Validate VenueOwner user-level subscription
    /// </summary>
    public async Task<(bool isActive, string? errorMessage)> ValidateVenueOwnerSubscriptionAsync(int userId, string? featureCode = null)
    {
        try
        {
            _logger.LogInformation("Validating VenueOwner subscription for user ID: {UserId}", userId);

            // 1. Find VenueOwnerProfile by userId
            var venueOwner = await _context.VenueOwnerProfiles
                .FirstOrDefaultAsync(vo => 
                    vo.UserId == userId && 
                    vo.IsDeleted != true);

            if (venueOwner == null)
            {
                _logger.LogWarning("Venue owner profile not found for user ID: {UserId}", userId);
                return (false, "Venue owner profile not found");
            }

            // 2. Check user-level subscription (OwnerId = venueOwner.Id, VenueId = null)
            var now = DateTime.UtcNow;
            var userLevelSub = await _context.VenueSubscriptionPackages
                .Where(vsp => 
                    vsp.OwnerId == venueOwner.Id &&
                    vsp.VenueId == null &&  // User-level subscription
                    vsp.Status == VenueSubscriptionPackageStatus.ACTIVE.ToString() &&
                    vsp.EndDate.HasValue &&
                    vsp.EndDate.Value >= now
                )
                .Include(vsp => vsp.Package)
                .OrderByDescending(vsp => vsp.EndDate)
                .FirstOrDefaultAsync();

            if (userLevelSub == null)
            {
                _logger.LogInformation(
                    "No active user-level subscription found for venue owner ID: {OwnerId} (User ID: {UserId})", 
                    venueOwner.Id, 
                    userId);
                return (false, "Gói của bạn đã hết hạn để sài tính năng này");
            }

            _logger.LogInformation(
                "Active user-level subscription found for venue owner ID: {OwnerId}, Subscription ID: {SubId}, EndDate: {EndDate}", 
                venueOwner.Id, 
                userLevelSub.Id,
                userLevelSub.EndDate);

            if (!HasFeatureAccess(userLevelSub.Package?.FeatureFlags, featureCode))
            {
                _logger.LogInformation(
                    "Feature access denied for venue owner ID: {OwnerId}, FeatureCode: {FeatureCode}, PackageId: {PackageId}",
                    venueOwner.Id,
                    featureCode,
                    userLevelSub.PackageId);
                return (false, "Gói hiện tại không hỗ trợ tính năng này");
            }

            return (true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating VenueOwner subscription for user ID: {UserId}", userId);
            throw;
        }
    }

    private bool HasFeatureAccess(JsonDocument? featureFlags, string? featureCode)
    {
        if (string.IsNullOrWhiteSpace(featureCode))
        {
            return true;
        }

        if (featureFlags == null || featureFlags.RootElement.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        var normalizedFeature = featureCode.Trim();
        foreach (var property in featureFlags.RootElement.EnumerateObject())
        {
            if (!string.Equals(property.Name, normalizedFeature, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            return property.Value.ValueKind switch
            {
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.String => bool.TryParse(property.Value.GetString(), out var boolValue) && boolValue,
                JsonValueKind.Number => property.Value.TryGetInt32(out var numberValue) && numberValue > 0,
                _ => false
            };
        }

        return false;
    }
}
