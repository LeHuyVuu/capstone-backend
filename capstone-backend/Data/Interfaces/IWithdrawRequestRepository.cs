using capstone_backend.Business.Interfaces;
using capstone_backend.Data.Entities;

namespace capstone_backend.Data.Interfaces;

public interface IWithdrawRequestRepository : IGenericRepository<WithdrawRequest>
{
    Task<List<WithdrawRequest>> GetByWalletIdAsync(int walletId);
}
