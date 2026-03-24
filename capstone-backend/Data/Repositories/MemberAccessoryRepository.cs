using capstone_backend.Data.Context;
using capstone_backend.Data.Entities;
using capstone_backend.Data.Interfaces;

namespace capstone_backend.Data.Repositories
{
    public class MemberAccessoryRepository : GenericRepository<MemberAccessory>, IMemberAccessoryRepository
    {
        public MemberAccessoryRepository(MyDbContext context) : base(context)
        {
        }
    }
}
