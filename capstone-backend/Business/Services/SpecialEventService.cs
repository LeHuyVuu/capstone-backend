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
        var specialEvent = new special_event()
        {
            event_name = request.EventName,
            description = request.Description,
            start_date = request.StartDate,
            end_date = request.EndDate,
            created_at = DateTime.UtcNow,
            updated_at = DateTime.UtcNow,
            is_deleted = false
        };

        await _unitOfWork.Context.Set<special_event>().AddAsync(specialEvent, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created special event {EventId} - {EventName}", specialEvent.id, specialEvent.event_name);

        return MapToResponse(specialEvent);
    }

    public async Task<SpecialEventResponse?> GetSpecialEventByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var specialEvent = await _unitOfWork.Context.Set<special_event>()
            .FirstOrDefaultAsync(e => e.id == id && e.is_deleted != true, cancellationToken);

        return specialEvent == null ? null : MapToResponse(specialEvent);
    }

    public async Task<PagedResult<SpecialEventResponse>> GetAllSpecialEventsAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = _unitOfWork.Context.Set<special_event>()
            .Where(e => e.is_deleted != true);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(e => e.start_date)
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
        var events = await _unitOfWork.Context.Set<special_event>()
            .Where(e => e.is_deleted != true && 
                       e.start_date <= now && 
                       e.end_date >= now)
            .OrderBy(e => e.start_date)
            .ToListAsync(cancellationToken);

        return events.Select(MapToResponse).ToList();
    }

    public async Task<SpecialEventResponse?> UpdateSpecialEventAsync(int id, UpdateSpecialEventRequest request, CancellationToken cancellationToken = default)
    {
        var specialEvent = await _unitOfWork.Context.Set<special_event>()
            .FirstOrDefaultAsync(e => e.id == id && e.is_deleted != true, cancellationToken);

        if (specialEvent == null)
            return null;

        if (!string.IsNullOrEmpty(request.EventName))
            specialEvent.event_name = request.EventName;

        if (request.Description != null)
            specialEvent.description = request.Description;

        if (request.StartDate.HasValue)
            specialEvent.start_date = request.StartDate.Value;

        if (request.EndDate.HasValue)
            specialEvent.end_date = request.EndDate.Value;

        specialEvent.updated_at = DateTime.UtcNow;

        _unitOfWork.Context.Set<special_event>().Update(specialEvent);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Updated special event {EventId}", id);

        return MapToResponse(specialEvent);
    }

    public async Task<bool> DeleteSpecialEventAsync(int id, CancellationToken cancellationToken = default)
    {
        var specialEvent = await _unitOfWork.Context.Set<special_event>()
            .FirstOrDefaultAsync(e => e.id == id && e.is_deleted != true, cancellationToken);

        if (specialEvent == null)
            return false;

        specialEvent.is_deleted = true;
        specialEvent.updated_at = DateTime.UtcNow;

        _unitOfWork.Context.Set<special_event>().Update(specialEvent);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Deleted special event {EventId}", id);

        return true;
    }

    private SpecialEventResponse MapToResponse(special_event specialEvent)
    {
        return new SpecialEventResponse
        {
            Id = specialEvent.id,
            EventName = specialEvent.event_name,
            Description = specialEvent.description,
            StartDate = specialEvent.start_date,
            EndDate = specialEvent.end_date,
            CreatedAt = specialEvent.created_at,
            UpdatedAt = specialEvent.updated_at
        };
    }
}
