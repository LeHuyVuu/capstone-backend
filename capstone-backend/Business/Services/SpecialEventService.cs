using capstone_backend.Business.DTOs.Common;
using capstone_backend.Business.DTOs.SpecialEvent;
using capstone_backend.Business.Interfaces;
using capstone_backend.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace capstone_backend.Business.Services;

public class SpecialEventService : ISpecialEventService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<SpecialEventService> _logger;

    public SpecialEventService(IUnitOfWork unitOfWork, ILogger<SpecialEventService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<SpecialEventResponse> CreateSpecialEventAsync(CreateSpecialEventRequest request, CancellationToken cancellationToken = default)
    {
        // Nếu là sự kiện hằng năm, normalize về năm 2000 nhưng giữ nguyên giờ phút
        var startDate = request.IsYearly 
            ? new DateTime(2000, request.StartDate.Month, request.StartDate.Day, 
                          request.StartDate.Hour, request.StartDate.Minute, request.StartDate.Second)
            : request.StartDate;
        
        var endDate = request.IsYearly 
            ? new DateTime(2000, request.EndDate.Month, request.EndDate.Day,
                          request.EndDate.Hour, request.EndDate.Minute, request.EndDate.Second)
            : request.EndDate;

        var specialEvent = new SpecialEvent()
        {
            EventName = request.EventName,
            Description = request.Description,            
            BannerUrl = request.BannerUrl,
            StartDate = startDate,
            EndDate = endDate,
            IsYearly = request.IsYearly,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsDeleted = false
        };

        await _unitOfWork.Context.Set<SpecialEvent>().AddAsync(specialEvent, cancellationToken);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Created special event {EventId} - {EventName} (IsYearly: {IsYearly})", 
            specialEvent.Id, specialEvent.EventName, specialEvent.IsYearly);

        return MapToResponse(specialEvent);
    }

    public async Task<SpecialEventResponse?> GetSpecialEventByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var specialEvent = await _unitOfWork.Context.Set<SpecialEvent>()
            .FirstOrDefaultAsync(e => e.Id == id && e.IsDeleted != true, cancellationToken);

        return specialEvent == null ? null : MapToResponse(specialEvent);
    }

    public async Task<PagedResult<SpecialEventResponse>> GetAllSpecialEventsAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = _unitOfWork.Context.Set<SpecialEvent>()
            .Where(e => e.IsDeleted != true);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(e => e.StartDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<SpecialEventResponse>
        {
            Items = items.Select(MapToResponse).ToList(),
            PageNumber = page,
            PageSize = pageSize,
            TotalCount = total
        };
    }

    public async Task<List<SpecialEventResponse>> GetActiveSpecialEventsAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var currentMonth = now.Month;
        var currentDay = now.Day;
        
        var events = await _unitOfWork.Context.Set<SpecialEvent>()
            .Where(e => e.IsDeleted != true)
            .ToListAsync(cancellationToken);

        // Filter events based on IsYearly flag
        var activeEvents = events.Where(e =>
        {
            if (e.IsYearly == true)
            {
                // So sánh theo ngày/tháng cho sự kiện hằng năm
                var startMonth = e.StartDate?.Month ?? 0;
                var startDay = e.StartDate?.Day ?? 0;
                var endMonth = e.EndDate?.Month ?? 0;
                var endDay = e.EndDate?.Day ?? 0;

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
                return e.StartDate <= now && e.EndDate >= now;
            }
        })
        .OrderBy(e => e.StartDate)
        .ToList();

        return activeEvents.Select(MapToResponse).ToList();
    }

    public async Task<SpecialEventResponse?> UpdateSpecialEventAsync(int id, UpdateSpecialEventRequest request, CancellationToken cancellationToken = default)
    {
        var specialEvent = await _unitOfWork.Context.Set<SpecialEvent>()
            .FirstOrDefaultAsync(e => e.Id == id && e.IsDeleted != true, cancellationToken);

        if (specialEvent == null)
            return null;

        if (!string.IsNullOrEmpty(request.EventName))
            specialEvent.EventName = request.EventName;

        if (request.Description != null)
            specialEvent.Description = request.Description;

        if (request.BannerUrl != null)
            specialEvent.BannerUrl = request.BannerUrl;

        if (request.IsYearly.HasValue)
            specialEvent.IsYearly = request.IsYearly.Value;

        // Xử lý StartDate dựa trên IsYearly
        if (request.StartDate.HasValue)
        {
            var isYearly = request.IsYearly ?? specialEvent.IsYearly ?? false;
            specialEvent.StartDate = isYearly 
                ? new DateTime(2000, request.StartDate.Value.Month, request.StartDate.Value.Day,
                              request.StartDate.Value.Hour, request.StartDate.Value.Minute, request.StartDate.Value.Second)
                : request.StartDate.Value;
        }

        // Xử lý EndDate dựa trên IsYearly
        if (request.EndDate.HasValue)
        {
            var isYearly = request.IsYearly ?? specialEvent.IsYearly ?? false;
            specialEvent.EndDate = isYearly 
                ? new DateTime(2000, request.EndDate.Value.Month, request.EndDate.Value.Day,
                              request.EndDate.Value.Hour, request.EndDate.Value.Minute, request.EndDate.Value.Second)
                : request.EndDate.Value;
        }

        specialEvent.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.Context.Set<SpecialEvent>().Update(specialEvent);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Updated special event {EventId}", id);

        return MapToResponse(specialEvent);
    }

    public async Task<bool> DeleteSpecialEventAsync(int id, CancellationToken cancellationToken = default)
    {
        var specialEvent = await _unitOfWork.Context.Set<SpecialEvent>()
            .FirstOrDefaultAsync(e => e.Id == id && e.IsDeleted != true, cancellationToken);

        if (specialEvent == null)
            return false;

        specialEvent.IsDeleted = true;
        specialEvent.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.Context.Set<SpecialEvent>().Update(specialEvent);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Deleted special event {EventId}", id);

        return true;
    }

    private SpecialEventResponse MapToResponse(SpecialEvent specialEvent)
    {
        return new SpecialEventResponse
        {
            Id = specialEvent.Id,
            EventName = specialEvent.EventName,
            Description = specialEvent.Description,
            BannerUrl = specialEvent.BannerUrl,
            StartDate = specialEvent.StartDate,
            EndDate = specialEvent.EndDate,
            IsYearly = specialEvent.IsYearly
        };
    }
}
