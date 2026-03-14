using capstone_backend.Business.DTOs.VenueOwner;

namespace capstone_backend.Business.Interfaces;

public interface IVenueOwnerDashboardService
{
    /// <summary>
    /// Lấy dashboard overview cho venue owner
    /// </summary>
    Task<VenueOwnerDashboardResponse> GetDashboardOverviewAsync(int userId);
    
    /// <summary>
    /// Lấy chi tiết analytics cho 1 venue cụ thể
    /// </summary>
    Task<VenueAnalyticsResponse> GetVenueAnalyticsAsync(int userId, int venueId, int days = 30);
}
