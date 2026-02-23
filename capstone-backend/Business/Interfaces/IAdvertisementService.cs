using capstone_backend.Business.DTOs.Advertisement;

namespace capstone_backend.Business.Interfaces;

public interface IAdvertisementService
{
    Task<List<AdvertisementResponse>> GetRotatingAdvertisementsAsync(string? placementType = null);
}
