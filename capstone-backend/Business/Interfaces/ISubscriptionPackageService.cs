using capstone_backend.Business.DTOs.SubscriptionPackage;

namespace capstone_backend.Business.Interfaces;

public interface ISubscriptionPackageService
{
    /// <summary>
    /// Get all subscription packages by type (MEMBER or VENUE)
    /// </summary>
    /// <param name="type">Package type: MEMBER/VENUEOWNER or VENUE</param>
    /// <returns>List of subscription packages</returns>
    Task<List<SubscriptionPackageDto>> GetSubscriptionPackagesByTypeAsync(string type);

    /// <summary>
    /// Update an existing subscription package
    /// </summary>
    /// <param name="id">Package ID</param>
    /// <param name="request">Update request data</param>
    /// <returns>Updated subscription package</returns>
    Task<SubscriptionPackageDto> UpdateSubscriptionPackageAsync(int id, UpdateSubscriptionPackageRequest request);

    /// <summary>
    /// Get venue subscription packages by venue ID
    /// </summary>
    /// <param name="venueId">Venue ID</param>
    /// <returns>List of venue subscription packages</returns>
    Task<List<VenueSubscriptionPackageDto>> GetVenueSubscriptionPackagesByVenueIdAsync(int venueId);

    /// <summary>
    /// Get all venue subscription packages for a venue owner by user ID
    /// </summary>
    /// <param name="userId">User ID of the venue owner</param>
    /// <returns>List of venue subscription packages for all venues owned by the user</returns>
    Task<List<VenueSubscriptionPackageDto>> GetVenueSubscriptionPackagesByOwnerUserIdAsync(int userId);

    Task<List<SubscriptionPackageDto>> GetAdminSubscriptionPackagesAsync(string? type, bool includeDeleted = false);

    Task<SubscriptionPackageDto?> GetAdminSubscriptionPackageByIdAsync(int id);

    Task<SubscriptionPackageDto> CreateSubscriptionPackageAsync(CreateSubscriptionPackageRequest request);

    Task<SubscriptionPackageDto> UpdateAdminSubscriptionPackageAsync(int id, UpdateSubscriptionPackageRequest request);

    Task DeleteSubscriptionPackageAsync(int id);
}
