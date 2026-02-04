using capstone_backend.Data.Context;
using capstone_backend.Data.Entities;
using capstone_backend.Data.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace capstone_backend.Data.Repositories
{
    public class NotificationRepository : GenericRepository<Notification>, INotificationRepository
    {
        public NotificationRepository(MyDbContext context) : base(context)
        {
        }

        public async Task<Notification?> GetByIdAndUserIdAsync(int notificationId, int userId)
        {
            return await _dbSet
                .AsNoTracking()
                .Where(n => n.Id == notificationId && n.UserId == userId)
                .FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<Notification>> GetUnreadNotificationsByUserIdAsync(int userId)
        {
            return await _dbSet
                .AsNoTracking()
                .Where(n => n.UserId == userId && n.IsRead == false)
                .ToListAsync();
        }
    }
}
