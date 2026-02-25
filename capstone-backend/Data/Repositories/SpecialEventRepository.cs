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
                && e.StartDate.HasValue
                && e.EndDate.HasValue
                && (
                    // Nếu là sự kiện hằng năm, chỉ so sánh ngày/tháng
                    (e.IsYearly == true
                        && now.Month >= e.StartDate.Value.Month
                        && now.Month <= e.EndDate.Value.Month
                        && now.Day >= e.StartDate.Value.Day
                        && now.Day <= e.EndDate.Value.Day)
                    ||
                    // Nếu không, so sánh đầy đủ như cũ
                    (e.IsYearly != true
                        && e.StartDate.Value <= now
                        && e.EndDate.Value >= now)
                )
            )
            .OrderBy(e => e.StartDate)
            .ToListAsync();
    }
}
