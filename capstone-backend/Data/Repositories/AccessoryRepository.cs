using capstone_backend.Data.Context;
using capstone_backend.Data.Entities;
using capstone_backend.Data.Interfaces;

namespace capstone_backend.Data.Repositories
{
    public class AccessoryRepository : GenericRepository<Accessory>, IAccessoryRepository
    {
        public AccessoryRepository(MyDbContext context) : base(context)
        {
        }
    }
}
