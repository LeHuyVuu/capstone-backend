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
        // Sử dụng DateTime.Now thay vì UtcNow vì database lưu với timezone +07
        var now = DateTime.Now;
        var currentMonth = now.Month;
        var currentDay = now.Day;

        // Lấy tất cả events chưa bị xóa
        var allEvents = await _dbSet
            .Where(e => e.IsDeleted == false)
            .ToListAsync();

        // Filter in-memory để xử lý logic phức tạp
        var activeEvents = allEvents.Where(e =>
        {
            if (!e.StartDate.HasValue || !e.EndDate.HasValue)
                return false;

            if (e.IsYearly == true)
            {
                // So sánh theo ngày/tháng cho sự kiện hằng năm
                var startMonth = e.StartDate.Value.Month;
                var startDay = e.StartDate.Value.Day;
                var endMonth = e.EndDate.Value.Month;
                var endDay = e.EndDate.Value.Day;

                // Xử lý trường hợp event cross-year (vd: 20/12 - 5/1)
                if (endMonth < startMonth || (endMonth == startMonth && endDay < startDay))
                {
                    return (currentMonth > startMonth || (currentMonth == startMonth && currentDay >= startDay)) ||
                           (currentMonth < endMonth || (currentMonth == endMonth && currentDay <= endDay));
                }

                // Trường hợp bình thường trong cùng năm
                return (currentMonth > startMonth || (currentMonth == startMonth && currentDay >= startDay)) &&
                       (currentMonth < endMonth || (currentMonth == endMonth && currentDay <= endDay));
            }
            else
            {
                // So sánh đầy đủ cho sự kiện một lần
                // Chuyển về local time để so sánh chính xác
                var startDateLocal = e.StartDate.Value.ToLocalTime();
                var endDateLocal = e.EndDate.Value.ToLocalTime();
                return startDateLocal <= now && endDateLocal >= now;
            }
        })
        .OrderBy(e => e.StartDate)
        .ToList();

        return activeEvents;
    }
}
