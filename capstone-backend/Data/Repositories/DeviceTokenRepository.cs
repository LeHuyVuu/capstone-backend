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

        public async Task<List<string>> GetByCoupleId(int coupleId)
        {
            var coupleUsers = await _context.CoupleProfiles
                .Where(c => c.id == coupleId && c.IsDeleted == false)
                .Select(c => new
                {
                    UserId1 = _context.MemberProfiles
                        .Where(m => m.Id == c.MemberId1 && m.IsDeleted == false)
                        .Select(m => m.UserId)
                        .FirstOrDefault(),

                    UserId2 = _context.MemberProfiles
                        .Where(m => m.Id == c.MemberId2 && m.IsDeleted == false)
                        .Select(m => m.UserId)
                        .FirstOrDefault(),
                })
                .FirstOrDefaultAsync();

            if (coupleUsers == null) 
                return new List<string>();

            var userIds = new[] { coupleUsers.UserId1, coupleUsers.UserId2 }
                .Where(x => x != 0)
                .Distinct()
                .ToList();

            if (!userIds.Any()) 
                return new List<string>();

            // Take device tokens for these users
            var tokens = await _dbSet
                .Where(dt => userIds.Contains(dt.UserId) && dt.IsDeleted == false)
                .Select(dt => dt.Token)
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .Distinct()
                .ToListAsync();

            return tokens;
        }

        public async Task<DeviceToken?> GetByTokenAsync(string token)
        {
            return await _dbSet
                .Where(dt => dt.Token == token && dt.IsDeleted == false)
                .FirstOrDefaultAsync();
        }
    }
}
