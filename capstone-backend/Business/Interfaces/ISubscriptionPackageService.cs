using capstone_backend.Business.DTOs.SubscriptionPackage;

namespace capstone_backend.Business.Interfaces;

public interface ISubscriptionPackageService
{
    /// <summary>
    /// Get all subscription packages by type (MEMBER or VENUE)
    /// </summary>
    /// <param name="type">Package type: MEMBER or VENUE</param>
    /// <returns>List of subscription packages</returns>
    Task<List<SubscriptionPackageDto>> GetSubscriptionPackagesByTypeAsync(string type);

    /// <summary>
    /// Update an existing subscription package
    /// </summary>
    /// <param name="id">Package ID</param>
    /// <param name="request">Update request data</param>
    /// <returns>Updated subscription package</returns>
    Task<SubscriptionPackageDto> UpdateSubscriptionPackageAsync(int id, UpdateSubscriptionPackageRequest request);
}
