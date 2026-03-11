using capstone_backend.Data.Context;
using capstone_backend.Data.Entities;
using capstone_backend.Data.Interfaces;

namespace capstone_backend.Data.Repositories
{
    public class VoucherItemMemberRepository : GenericRepository<VoucherItemMember>, IVoucherItemMemberRepository
    {
        public VoucherItemMemberRepository(MyDbContext context) : base(context)
        {
    }
}
