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

        public async Task<CheckInHistory?> GetLatestByMemberIdAndVenueIdAsync(int memberId, int venueId, int? delaySeconds = null)
        {
            var query = _dbSet
                .Where(c => c.MemberId == memberId && c.VenueId == venueId);

            if (delaySeconds.HasValue && delaySeconds.Value > 0)
            {
                var threshold = DateTime.UtcNow.AddSeconds(-delaySeconds.Value);
                query = query.Where(c => c.CreatedAt.HasValue && c.CreatedAt.Value >= threshold);
            }

            return await query
                .OrderByDescending(c => c.CreatedAt)
                .FirstOrDefaultAsync();
        }
    }
}
