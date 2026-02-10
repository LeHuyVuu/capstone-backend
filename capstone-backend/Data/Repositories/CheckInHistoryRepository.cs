using capstone_backend.Data.Context;
using capstone_backend.Data.Entities;
using capstone_backend.Data.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace capstone_backend.Data.Repositories
{
    public class CheckInHistoryRepository : GenericRepository<CheckInHistory>, ICheckInHistoryRepository
    {
        public CheckInHistoryRepository(MyDbContext context) : base(context)
        {
        }

        public async Task<CheckInHistory?> GetLatestByMemberIdAndVenueIdAsync(int memberId, int venueId)
        {
            return await _dbSet
                .Where(c => c.MemberId == memberId && c.VenueId == venueId)
                .OrderByDescending(c => c.CreatedAt)
                .FirstOrDefaultAsync();
        }
    }
}
