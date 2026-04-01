using capstone_backend.Business.Interfaces;
using capstone_backend.Data.Entities;

namespace capstone_backend.Data.Interfaces
{
    public interface IMemberSubscriptionPackageRepository : IGenericRepository<MemberSubscriptionPackage>
    {
        Task<MemberSubscriptionPackage?> GetCurrentActiveSubscriptionAsync(int id);
        Task<IEnumerable<MemberSubscriptionPackage>> GetExpiredSubscriptionsAsync(DateTime now);
    }
}
