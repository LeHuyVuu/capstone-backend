using capstone_backend.Data.Context;
using capstone_backend.Data.Entities;
using capstone_backend.Data.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace capstone_backend.Data.Repositories;

public class WithdrawRequestRepository : GenericRepository<WithdrawRequest>, IWithdrawRequestRepository
{
    public WithdrawRequestRepository(MyDbContext context) : base(context)
    {
    }

    public async Task<List<WithdrawRequest>> GetByWalletIdAsync(int walletId)
    {
        return await _dbSet
            .Where(wr => wr.WalletId == walletId)
            .OrderByDescending(wr => wr.RequestedAt)
            .ToListAsync();
    }
}
