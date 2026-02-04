using capstone_backend.Business.Interfaces;
using capstone_backend.Data.Entities;

namespace capstone_backend.Data.Interfaces
{
    public interface INotificationRepository : IGenericRepository<Notification>
    {
        Task<Notification?> GetByIdAndUserIdAsync(int notificationId, int userId);
        Task<IEnumerable<Notification>> GetUnreadNotificationsByUserIdAsync(int userId);
    }
}
