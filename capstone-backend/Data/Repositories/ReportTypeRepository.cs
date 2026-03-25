using capstone_backend.Data.Context;
using capstone_backend.Data.Entities;
using capstone_backend.Data.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace capstone_backend.Data.Repositories;

public class ReportTypeRepository : GenericRepository<ReportType>, IReportTypeRepository
{
    public ReportTypeRepository(MyDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<ReportType>> GetActiveReportTypesAsync()
    {
        return await _context.Set<ReportType>()
            .Where(rt => rt.IsActive == true && rt.IsDeleted != true)
            .OrderBy(rt => rt.TypeName)
            .ToListAsync();
    }
}
