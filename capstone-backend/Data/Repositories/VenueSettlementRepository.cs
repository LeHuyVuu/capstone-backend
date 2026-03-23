using capstone_backend.Data.Context;
using capstone_backend.Data.Entities;
using capstone_backend.Data.Enums;
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

        public async Task<IEnumerable<VenueSettlement>> GetDueSettlementsAsync(DateTime now)
        {
            return await _dbSet
                .Where(vs => vs.IsDeleted == false &&
                       vs.Status == VenueSettlementStatus.PENDING.ToString() &&
                       vs.AvailableAt.HasValue &&
                       vs.AvailableAt.Value <= now
                ).ToListAsync();
        }
    }
}
