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

        public async Task<int> CountByTargetIdAndTypeAsync(int id, string type)
        {
            return await _dbSet
                .AsNoTracking()
                .Where(m => m.TargetId == id && m.TargetType == type && m.IsDeleted == false)
                .CountAsync();
        }

        public async Task<IEnumerable<Media>> GetAllDeletedAsync()
        {
            return await _dbSet
                .AsNoTracking()
                .Where(m => m.IsDeleted == true)
                .Take(100)
                .ToListAsync();
        }

        public async Task<IEnumerable<Media>> GetByListTargetIdsAsync(List<int> targetIds, string type)
        {
            return await _dbSet
                .AsNoTracking()
                .Where(m => targetIds.Contains(m.TargetId.Value) && m.TargetType == type && m.IsDeleted == false)
                .ToListAsync();
        }

        public async Task<IEnumerable<Media>> GetByTargetIdAndTypeAsync(int id, string type)
        {
            return await _dbSet
                .AsNoTracking()
                .Where(m => m.TargetId == id && m.TargetType == type && m.IsDeleted == false)
                .ToListAsync();
        }

        public async Task<IEnumerable<Media>> GetByUrlsAsync(List<string> urls)
        {
            return await _dbSet
                .AsNoTracking()
                .Where(m => urls.Contains(m.Url) && m.IsDeleted == false)
                .ToListAsync();
        }
    }
}
