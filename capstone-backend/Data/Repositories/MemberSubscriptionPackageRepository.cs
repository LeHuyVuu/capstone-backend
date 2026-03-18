using capstone_backend.Data.Context;
using capstone_backend.Data.Entities;
using capstone_backend.Data.Interfaces;

namespace capstone_backend.Data.Repositories
{
    public class MemberSubscriptionPackageRepository : GenericRepository<MemberSubscriptionPackage>, IMemberSubscriptionPackageRepository
    {
        public MemberSubscriptionPackageRepository(MyDbContext context) : base(context)
        {
        }
    }
}
