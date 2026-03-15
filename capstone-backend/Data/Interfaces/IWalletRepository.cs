using capstone_backend.Business.Interfaces;
using capstone_backend.Data.Entities;

namespace capstone_backend.Data.Interfaces;

public interface IWalletRepository : IGenericRepository<Wallet>
{
    Task<Wallet?> GetByUserIdAsync(int userId);
}
