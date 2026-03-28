namespace capstone_backend.Business.Interfaces;

/// <summary>
/// Service for validating user subscription packages
/// </summary>
public interface ISubscriptionValidationService
{
    /// <summary>
    /// Validate subscription based on user type (MEMBER or VENUE_OWNER)
    /// </summary>
    /// <param name="userId">User ID from authentication claims</param>
    /// <param name="userType">User type: "MEMBER" or "VENUE_OWNER"</param>
    /// <returns>
    /// Tuple of (isActive, errorMessage)
    /// - isActive: true if user has active subscription
    /// - errorMessage: null if active, error message if not active
    /// </returns>
    Task<(bool isActive, string? errorMessage)> ValidateSubscriptionAsync(int userId, string userType);
    
    /// <summary>
    /// Validate Member subscription
    /// Checks MemberSubscriptionPackage with Status=ACTIVE and valid EndDate
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <returns>Tuple of (isActive, errorMessage)</returns>
    Task<(bool isActive, string? errorMessage)> ValidateMemberSubscriptionAsync(int userId);
    
    /// <summary>
    /// Validate VenueOwner user-level subscription
    /// Checks VenueSubscriptionPackage with OwnerId={ownerId} and VenueId=null
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <returns>Tuple of (isActive, errorMessage)</returns>
    Task<(bool isActive, string? errorMessage)> ValidateVenueOwnerSubscriptionAsync(int userId);
}
