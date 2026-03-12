using capstone_backend.Data.Context;
using capstone_backend.Data.Entities;
using capstone_backend.Data.Interfaces;

namespace capstone_backend.Data.Repositories
{
    public class VoucherJobRepository : GenericRepository<VoucherJob>, IVoucherJobRepository
    {
        public VoucherJobRepository(MyDbContext context) : base(context)
        {
        }
    }
}
