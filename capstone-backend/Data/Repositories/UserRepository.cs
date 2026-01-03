using capstone_backend.Business.Entities;
using capstone_backend.Business.Interfaces;
using capstone_backend.Data;
using Microsoft.EntityFrameworkCore;

namespace capstone_backend.Data.Repositories;

/// <summary>
/// User repository implementation
/// </summary>
/// <remarks>
/// Implements User-specific database operations.
/// </remarks>
public class UserRepository : Repository<User>, IUserRepository
{
    public UserRepository(AppDbContext context) : base(context)
    {
    }

    /// <inheritdoc/>
    public async Task<User?> GetByEmailAsync(
        string email,
        bool includeSoftDeleted = false,
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet.AsQueryable();

        if (!includeSoftDeleted)
            query = query.Where(u => !u.IsDeleted);

        return await query.FirstOrDefaultAsync(u => u.Email == email, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<bool> EmailExistsAsync(
        string email,
        Guid? excludeUserId = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet.Where(u => !u.IsDeleted && u.Email == email);

        if (excludeUserId.HasValue)
            query = query.Where(u => u.Id != excludeUserId.Value);

        return await query.AnyAsync(cancellationToken);
    }
}
