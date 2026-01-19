using capstone_backend.Business.Interfaces;
using capstone_backend.Data.Context;
using capstone_backend.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace capstone_backend.Data.Repositories;

/// <summary>
/// User repository implementation for user_account entity
/// </summary>
public class UserRepository : Repository<UserAccount>, IUserRepository
{
    public UserRepository(MyDbContext context) : base(context)
    {
    }

    public override async Task<UserAccount?> GetByIdAsync(
        int id,
        bool includeSoftDeleted = false,
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet.AsQueryable();
        
        if (!includeSoftDeleted)
            query = query.Where(u => u.IsDeleted != true);

        return await query.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
    }

    public async Task<UserAccount?> GetByEmailAsync(
        string email,
        bool includeSoftDeleted = false,
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet
            .Include(u => u.member_profiles)
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
            .Include(u => u.member_profiles)
            .Include(u => u.venue_owner_profiles)
            .AsQueryable();

        if (!includeSoftDeleted)
            query = query.Where(u => u.IsDeleted != true);

        return await query.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
    }

    public override void SoftDelete(UserAccount entity, int? deletedBy = null)
    {
        entity.IsDeleted = true;
        entity.UpdatedAt = DateTime.UtcNow;
        _dbSet.Update(entity);
    }
}
