using capstone_backend.Data.Context;
using capstone_backend.Data.Entities;
using capstone_backend.Data.Interfaces;

namespace capstone_backend.Data.Repositories
{
    public class AccessoryPurchaseRepository : GenericRepository<AccessoryPurchase>, IAccessoryPurchaseRepository
    {
        public AccessoryPurchaseRepository(MyDbContext context) : base(context)
        {
        }
    }
}
