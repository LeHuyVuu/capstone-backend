using capstone_backend.Data.Context;
using capstone_backend.Data.Entities;
using capstone_backend.Data.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace capstone_backend.Data.Repositories
{
    public class VenueSettlementRepository : GenericRepository<VenueSettlement>, IVenueSettlementRepository
    {
        public VenueSettlementRepository(MyDbContext context) : base(context)
        {
        }

        public IQueryable<VenueSettlement> GetByVenueOwnerId(int venueOwnerId)
        {
            return _dbSet
                .Where(vs => vs.VenueOwnerId == venueOwnerId && !vs.IsDeleted);
        }
    }
}
