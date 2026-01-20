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
        var specialEvent = new SpecialEvent()
        {
            EventName = request.EventName,
            Description = request.Description,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsDeleted = false
        };

        await _unitOfWork.Context.Set<SpecialEvent>().AddAsync(specialEvent, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created special event {EventId} - {EventName}", specialEvent.Id, specialEvent.EventName);

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
        var events = await _unitOfWork.Context.Set<SpecialEvent>()
            .Where(e => e.IsDeleted != true && 
                       e.StartDate <= now && 
                       e.EndDate >= now)
            .OrderBy(e => e.StartDate)
            .ToListAsync(cancellationToken);

        return events.Select(MapToResponse).ToList();
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

        if (request.StartDate.HasValue)
            specialEvent.StartDate = request.StartDate.Value;

        if (request.EndDate.HasValue)
            specialEvent.EndDate = request.EndDate.Value;

        specialEvent.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.Context.Set<SpecialEvent>().Update(specialEvent);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

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
        await _unitOfWork.SaveChangesAsync(cancellationToken);

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
            StartDate = specialEvent.StartDate,
            EndDate = specialEvent.EndDate,
            CreatedAt = specialEvent.CreatedAt,
            UpdatedAt = specialEvent.UpdatedAt
        };
    }
}
