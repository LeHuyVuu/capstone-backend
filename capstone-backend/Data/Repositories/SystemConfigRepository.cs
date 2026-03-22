using capstone_backend.Data.Context;
using capstone_backend.Data.Entities;
using capstone_backend.Data.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace capstone_backend.Data.Repositories
{
    public class SystemConfigRepository : GenericRepository<SystemConfig>, ISystemConfigRepository
    {
        public SystemConfigRepository(MyDbContext context) : base(context)
        {
        }

        public async Task<SystemConfig?> GetByKeyAsync(string key)
        {
            return await _dbSet
                .FirstOrDefaultAsync(sc => sc.IsDeleted == false && sc.ConfigKey == key);
        }
    }
}
