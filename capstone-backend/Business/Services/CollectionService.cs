using capstone_backend.Business.DTOs.Collection;
using capstone_backend.Business.DTOs.Common;
using capstone_backend.Business.Interfaces;
using capstone_backend.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace capstone_backend.Business.Services;

public class CollectionService : ICollectionService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CollectionService> _logger;

    public CollectionService(IUnitOfWork unitOfWork, ILogger<CollectionService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<CollectionResponse> CreateCollectionAsync(int memberId, CreateCollectionRequest request, CancellationToken cancellationToken = default)
    {
        var collection = new Collection()
        {
            MemberId = memberId,
            CollectionName = request.CollectionName,
            Description = request.Description,
            Status = request.Status,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsDeleted = false
        };

        await _unitOfWork.Context.Set<Collection>().AddAsync(collection, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created collection {CollectionId} for member {MemberId}", collection.Id, memberId);

        return MapToResponse(collection);
    }

    public async Task<CollectionResponse?> GetCollectionByIdAsync(int collectionId, CancellationToken cancellationToken = default)
    {
        var collection = await _unitOfWork.Context.Set<Collection>()
            .Include(c => c.Venues)
            .FirstOrDefaultAsync(c => c.Id == collectionId && c.IsDeleted != true, cancellationToken);

        return collection == null ? null : MapToResponse(collection);
    }

    public async Task<PagedResult<CollectionResponse>> GetCollectionsByMemberAsync(int memberId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = _unitOfWork.Context.Set<Collection>()
            .Include(c => c.Venues)
            .Where(c => c.MemberId == memberId && c.IsDeleted != true);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(c => c.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<CollectionResponse>
        {
            Items = items.Select(MapToResponse).ToList(),
            PageNumber = page,
            PageSize = pageSize,
            TotalCount = total
        };
    }

    public async Task<CollectionResponse?> UpdateCollectionAsync(int collectionId, int memberId, UpdateCollectionRequest request, CancellationToken cancellationToken = default)
    {
        var collection = await _unitOfWork.Context.Set<Collection>()
            .FirstOrDefaultAsync(c => c.Id == collectionId && c.MemberId == memberId && c.IsDeleted != true, cancellationToken);

        if (collection == null)
            return null;

        if (!string.IsNullOrEmpty(request.CollectionName))
            collection.CollectionName = request.CollectionName;
        
        if (request.Description != null)
            collection.Description = request.Description;
        
        if (!string.IsNullOrEmpty(request.Status))
            collection.Status = request.Status;

        collection.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.Context.Set<Collection>().Update(collection);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Updated collection {CollectionId}", collectionId);

        return MapToResponse(collection);
    }

    public async Task<bool> DeleteCollectionAsync(int collectionId, int memberId, CancellationToken cancellationToken = default)
    {
        var collection = await _unitOfWork.Context.Set<Collection>()
            .FirstOrDefaultAsync(c => c.Id == collectionId && c.MemberId == memberId && c.IsDeleted != true, cancellationToken);

        if (collection == null)
            return false;

        collection.IsDeleted = true;
        collection.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.Context.Set<Collection>().Update(collection);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Deleted collection {CollectionId}", collectionId);

        return true;
    }

    public async Task<CollectionResponse?> AddVenuesToCollectionAsync(int collectionId, int memberId, PatchCollectionRequest request, CancellationToken cancellationToken = default)
    {
        var collection = await _unitOfWork.Context.Set<Collection>()
            .Include(c => c.Venues)
            .FirstOrDefaultAsync(c => c.Id == collectionId && c.MemberId == memberId && c.IsDeleted != true, cancellationToken);

        if (collection == null)
            return null;

        var venuesToAdd = await _unitOfWork.Context.Set<VenueLocation>()
            .Where(v => request.VenueIds.Contains(v.Id))
            .ToListAsync(cancellationToken);

        foreach (var venue in venuesToAdd)
        {
            if (!collection.Venues.Any(v => v.Id == venue.Id))
            {
                collection.Venues.Add(venue);
            }
        }

        collection.UpdatedAt = DateTime.UtcNow;
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Added {Count} venues to collection {CollectionId}", venuesToAdd.Count, collectionId);

        return MapToResponse(collection);
    }

    public async Task<CollectionResponse?> RemoveVenuesFromCollectionAsync(int collectionId, int memberId, PatchCollectionRequest request, CancellationToken cancellationToken = default)
    {
        var collection = await _unitOfWork.Context.Set<Collection>()
            .Include(c => c.Venues)
            .FirstOrDefaultAsync(c => c.Id == collectionId && c.MemberId == memberId && c.IsDeleted != true, cancellationToken);

        if (collection == null)
            return null;

        var venuesToRemove = collection.Venues
            .Where(v => request.VenueIds.Contains(v.Id))
            .ToList();

        foreach (var venue in venuesToRemove)
        {
            collection.Venues.Remove(venue);
        }

        collection.UpdatedAt = DateTime.UtcNow;
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Removed {Count} venues from collection {CollectionId}", venuesToRemove.Count, collectionId);

        return MapToResponse(collection);
    }

    private CollectionResponse MapToResponse(Collection collection)
    {
        return new CollectionResponse
        {
            Id = collection.Id,
            MemberId = collection.MemberId,
            CollectionName = collection.CollectionName,
            Description = collection.Description,
            Status = collection.Status,
            CreatedAt = collection.CreatedAt,
            UpdatedAt = collection.UpdatedAt,
            Venues = collection.Venues?.Select(v => new VenueSimpleResponse
            {
                Id = v.Id,
                Name = v.Name,
                Description = v.Description,
                Address = v.Address
            }).ToList()
        };
    }
}
