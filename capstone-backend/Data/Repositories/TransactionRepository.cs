using capstone_backend.Data.Context;
using capstone_backend.Data.Entities;
using capstone_backend.Data.Enums;
using capstone_backend.Data.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace capstone_backend.Data.Repositories
{
    public class TransactionRepository : GenericRepository<Transaction>, ITransactionRepository
    {
        public TransactionRepository(MyDbContext context) : base(context)
        {
        }

        public async Task<Transaction?> GetRecentPendingAsync(int userId, int packageId, DateTime timeLimit)
        {
            var query = from t in _dbSet
                        join s in _context.Set<MemberSubscriptionPackage>() on t.DocNo equals s.Id
                        where t.UserId == userId
                              && t.TransType == 3
                              && t.PaymentMethod == PaymentMethod.MOMO.ToString()
                              && t.Status == TransactionStatus.PENDING.ToString()
                              && t.CreatedAt >= timeLimit
                              && s.PackageId == packageId
                        orderby t.CreatedAt descending
                        select t;

            return await query.FirstOrDefaultAsync();
        }
    }
}
