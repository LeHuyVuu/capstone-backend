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
}
