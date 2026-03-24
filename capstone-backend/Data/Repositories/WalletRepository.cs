using capstone_backend.Data.Context;
using capstone_backend.Data.Entities;
using capstone_backend.Data.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace capstone_backend.Data.Repositories;

public class WalletRepository : GenericRepository<Wallet>, IWalletRepository
{
    public WalletRepository(MyDbContext context) : base(context)
    {
    }

    public async Task<Wallet?> GetByUserIdAsync(int userId)
    {
        return await _dbSet
            .FirstOrDefaultAsync(w => w.UserId == userId);
    }

    public async Task<IEnumerable<Wallet>> GetByUserIdsAsync(List<int> userId)
    {
        return await _dbSet
            .Where(w => userId.Contains(w.UserId) && w.IsActive == true)
            .ToListAsync();
    }
}
