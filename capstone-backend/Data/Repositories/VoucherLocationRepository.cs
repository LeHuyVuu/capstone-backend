using capstone_backend.Data.Context;
using capstone_backend.Data.Entities;
using capstone_backend.Data.Interfaces;

namespace capstone_backend.Data.Repositories
{
    public class VoucherLocationRepository : GenericRepository<VoucherLocation>, IVoucherLocationRepository
    {
        public VoucherLocationRepository(MyDbContext context) : base(context)
        {
        }
    }
}
