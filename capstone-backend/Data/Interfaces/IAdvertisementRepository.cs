using capstone_backend.Business.Interfaces;
using capstone_backend.Data.Entities;

namespace capstone_backend.Data.Interfaces;

public interface IAdvertisementRepository : IGenericRepository<Advertisement>
{
    Task<List<VenueLocationAdvertisement>> GetActiveAdvertisementsAsync();
}
