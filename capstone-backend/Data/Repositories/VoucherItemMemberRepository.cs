using capstone_backend.Data.Context;
using capstone_backend.Data.Entities;
using capstone_backend.Data.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace capstone_backend.Data.Repositories
{
    public class VoucherItemMemberRepository : GenericRepository<VoucherItemMember>, IVoucherItemMemberRepository
    {
        public VoucherItemMemberRepository(MyDbContext context) : base(context)
        {
        }

        public async Task<VoucherItemMember?> GetIncludeByIdAsync(int memberId, int voucherItemMemberId)
        {
            return await _dbSet
                .Include(vim => vim.VoucherItems)
                    .ThenInclude(vi => vi.Voucher)
                .FirstOrDefaultAsync(vim => vim.Id == voucherItemMemberId && vim.MemberId == memberId);
        }
    }
}
