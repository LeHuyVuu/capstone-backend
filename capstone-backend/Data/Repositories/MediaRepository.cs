using capstone_backend.Data.Context;
using capstone_backend.Data.Entities;
using capstone_backend.Data.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace capstone_backend.Data.Repositories
{
    public class MediaRepository : GenericRepository<Media>, IMediaRepository
    {
        public MediaRepository(MyDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<Media>> GetByListTargetIdsAsync(List<int> targetIds, string type)
        {
            return await _dbSet
                .AsNoTracking()
                .Where(m => targetIds.Contains(m.TargetId) && m.TargetType == type)
                .ToListAsync();
        }
    }
}
