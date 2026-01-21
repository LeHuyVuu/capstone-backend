using capstone_backend.Business.Interfaces;
using capstone_backend.Data.Context;
using capstone_backend.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace capstone_backend.Data.Repositories;

/// <summary>
/// User repository implementation for user_account entity
/// </summary>
public class UserRepository : GenericRepository<UserAccount>, IUserRepository
{
    public UserRepository(MyDbContext context) : base(context)
    {
    }

    public async Task<UserAccount?> GetByIdAsync(int id)
    {
        var query = _dbSet.AsQueryable();
        
        query = query.Where(u => u.IsDeleted != true);

        return await query.FirstOrDefaultAsync(u => u.Id == id);
    }

    public async Task<UserAccount?> GetByEmailAsync(
        string email,
        bool includeSoftDeleted = false,
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet
            .Include(u => u.MemberProfiles)
            .AsQueryable();

        if (!includeSoftDeleted)
            query = query.Where(u => u.IsDeleted != true);

        return await query.FirstOrDefaultAsync(u => u.Email == email, cancellationToken);
    }

    public async Task<bool> EmailExistsAsync(
        string email,
        int? excludeUserId = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet.Where(u => u.IsDeleted != true && u.Email == email);

        if (excludeUserId.HasValue)
            query = query.Where(u => u.Id != excludeUserId.Value);

        return await query.AnyAsync(cancellationToken);
    }

    public async Task<UserAccount?> GetByIdWithProfilesAsync(
        int id,
        bool includeSoftDeleted = false,
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet
            .Include(u => u.MemberProfiles)
            .Include(u => u.VenueOwnerProfiles)
            .AsQueryable();

        if (!includeSoftDeleted)
            query = query.Where(u => u.IsDeleted != true);

        return await query.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
    }
}
