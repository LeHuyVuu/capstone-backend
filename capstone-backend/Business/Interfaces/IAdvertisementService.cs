using capstone_backend.Business.DTOs.Advertisement;

namespace capstone_backend.Business.Interfaces;

public interface IAdvertisementService
{
    Task<List<AdvertisementResponse>> GetRotatingAdvertisementsAsync(string? placementType = null);
    
    // Venue owner advertisement management (userId is UserAccount.Id, service will find VenueOwnerProfile.Id)
    Task<AdvertisementDetailResponse> CreateAdvertisementAsync(CreateAdvertisementRequest request, int userId);
    Task<List<MyAdvertisementResponse>> GetMyAdvertisementsAsync(int userId);
    Task<AdvertisementDetailResponse?> GetAdvertisementByIdAsync(int id, int userId);
    Task<SubmitAdvertisementWithPaymentResponse> SubmitAdvertisementWithPaymentAsync(int advertisementId, int userId, SubmitAdvertisementWithPaymentRequest request);
    
    // Advertisement packages
    Task<List<AdvertisementPackageResponse>> GetAdvertisementPackagesAsync();
}
