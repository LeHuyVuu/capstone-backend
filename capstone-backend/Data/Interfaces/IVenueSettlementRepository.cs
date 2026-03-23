using capstone_backend.Business.Interfaces;
using capstone_backend.Data.Entities;

namespace capstone_backend.Data.Interfaces
{
    public interface IVenueSettlementRepository : IGenericRepository<VenueSettlement>
    {
        IQueryable<VenueSettlement> GetByVenueOwnerId(int venueOwnerId);
        Task<IEnumerable<VenueSettlement>> GetDueSettlementsAsync(DateTime now);
    }
}
