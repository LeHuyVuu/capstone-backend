using capstone_backend.Data.Context;
using capstone_backend.Data.Entities;
using capstone_backend.Data.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace capstone_backend.Data.Repositories
{
    public class DeviceTokenRepository : GenericRepository<DeviceToken>, IDeviceTokenRepository
    {
        public DeviceTokenRepository(MyDbContext context) : base(context)
        {
        }

        public async Task<DeviceToken?> GetByTokenAsync(string token)
        {
            return await _dbSet
                .Where(dt => dt.Token == token && dt.IsDeleted == false)
                .FirstOrDefaultAsync();
        }
    }
}
