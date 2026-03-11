using capstone_backend.Data.Context;
using capstone_backend.Data.Entities;
using capstone_backend.Data.Interfaces;

namespace capstone_backend.Data.Repositories
{
    public class VoucherItemRepository : GenericRepository<VoucherItem>, IVoucherItemRepository
    {
        public VoucherItemRepository(MyDbContext context) : base(context)
        {
        }
    }
}
