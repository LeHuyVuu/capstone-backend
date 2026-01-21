using capstone_backend.Business.Interfaces;
using capstone_backend.Data.Context;
using Microsoft.EntityFrameworkCore.Storage;

namespace capstone_backend.Data.Repositories;

/// <summary>
/// Unit of Work implementation for coordinating repository operations
/// </summary>
public class UnitOfWork : IUnitOfWork
{
    private readonly MyDbContext _context;
    private IDbContextTransaction? _transaction;

    public UnitOfWork(MyDbContext context, IUserRepository userRepository, IMemberProfileRepository memberProfileRepository)
    {
        _context = context;
        Users = userRepository;
        MembersProfile = memberProfileRepository;
    }

    public MyDbContext Context => _context;

    public IUserRepository Users { get; }

    public IMemberProfileRepository MembersProfile { get; }

    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }

    public async Task BeginTransactionAsync()
    {
        _transaction = await _context.Database.BeginTransactionAsync();
    }

    public async Task CommitTransactionAsync()
    {
        try
        {
            await SaveChangesAsync();
            if (_transaction != null)
            {
                await _transaction.CommitAsync();
            }
        }
        catch
        {
            await RollbackTransactionAsync();
            throw;
        }
        finally
        {
            if (_transaction != null)
            {
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }
    }

    public async Task RollbackTransactionAsync()
    {
        if (_transaction != null)
        {
            await _transaction.RollbackAsync();
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public void Dispose()
    {
        _transaction?.Dispose();
        _context.Dispose();
    }
}
