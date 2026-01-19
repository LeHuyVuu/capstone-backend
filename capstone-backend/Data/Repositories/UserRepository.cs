using capstone_backend.Business.Interfaces;
using capstone_backend.Data.Context;
using capstone_backend.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace capstone_backend.Data.Repositories;

/// <summary>
/// User repository implementation for user_account entity
/// </summary>
public class UserRepository : Repository<user_account>, IUserRepository
{
    public UserRepository(MyDbContext context) : base(context)
    {
    }

    public override async Task<user_account?> GetByIdAsync(
        int id,
        bool includeSoftDeleted = false,
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet.AsQueryable();
        
        if (!includeSoftDeleted)
            query = query.Where(u => u.is_deleted != true);

        return await query.FirstOrDefaultAsync(u => u.id == id, cancellationToken);
    }

    public async Task<user_account?> GetByEmailAsync(
        string email,
        bool includeSoftDeleted = false,
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet
            .Include(u => u.member_profiles)
            .AsQueryable();

        if (!includeSoftDeleted)
            query = query.Where(u => u.is_deleted != true);

        return await query.FirstOrDefaultAsync(u => u.email == email, cancellationToken);
    }

    public async Task<bool> EmailExistsAsync(
        string email,
        int? excludeUserId = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet.Where(u => u.is_deleted != true && u.email == email);

        if (excludeUserId.HasValue)
            query = query.Where(u => u.id != excludeUserId.Value);

        return await query.AnyAsync(cancellationToken);
    }

    public async Task<user_account?> GetByIdWithProfilesAsync(
        int id,
        bool includeSoftDeleted = false,
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet
            .Include(u => u.member_profiles)
            .Include(u => u.venue_owner_profiles)
            .AsQueryable();

        if (!includeSoftDeleted)
            query = query.Where(u => u.is_deleted != true);

        return await query.FirstOrDefaultAsync(u => u.id == id, cancellationToken);
    }

    public override void SoftDelete(user_account entity, int? deletedBy = null)
    {
        entity.is_deleted = true;
        entity.updated_at = DateTime.UtcNow;
        _dbSet.Update(entity);
    }
}
