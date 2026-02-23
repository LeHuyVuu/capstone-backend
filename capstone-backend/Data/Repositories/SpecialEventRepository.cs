using capstone_backend.Data.Context;
using capstone_backend.Data.Entities;
using capstone_backend.Data.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace capstone_backend.Data.Repositories;

public class SpecialEventRepository : GenericRepository<SpecialEvent>, ISpecialEventRepository
{
    public SpecialEventRepository(MyDbContext context) : base(context)
    {
    }

    public async Task<List<SpecialEvent>> GetActiveSpecialEventsAsync()
    {
        var now = DateTime.UtcNow;

        return await _dbSet
            .Where(e => 
                e.IsDeleted == false 
                // e.StartDate <= now &&
                // e.EndDate >= now
            )
            .OrderBy(e => e.StartDate)
            .ToListAsync();
    }
}
