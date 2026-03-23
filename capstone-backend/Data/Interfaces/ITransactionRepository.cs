using capstone_backend.Business.Interfaces;
using capstone_backend.Data.Entities;

namespace capstone_backend.Data.Interfaces
{
    public interface ITransactionRepository : IGenericRepository<Transaction>
    {
        Task<Transaction?> GetRecentPendingAsync(int userId, int packageId, DateTime timeLimit);
        Task<Transaction?> GetWalletTopupPendingAsync(int userId, DateTime timeLimit);
    }
}
