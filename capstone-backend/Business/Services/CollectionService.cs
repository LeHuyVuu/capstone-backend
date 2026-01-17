using capstone_backend.Business.DTOs.Collection;
using capstone_backend.Business.DTOs.Common;
using capstone_backend.Business.Interfaces;
using capstone_backend.Entities;
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
        var collection = new collection
        {
            member_id = memberId,
            collection_name = request.CollectionName,
            description = request.Description,
            status = request.Status,
            created_at = DateTime.UtcNow,
            updated_at = DateTime.UtcNow,
            is_deleted = false
        };

        await _unitOfWork.Context.Set<collection>().AddAsync(collection, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created collection {CollectionId} for member {MemberId}", collection.id, memberId);

        return MapToResponse(collection);
    }

    public async Task<CollectionResponse?> GetCollectionByIdAsync(int collectionId, CancellationToken cancellationToken = default)
    {
        var collection = await _unitOfWork.Context.Set<collection>()
            .Include(c => c.venues)
            .FirstOrDefaultAsync(c => c.id == collectionId && c.is_deleted != true, cancellationToken);

        return collection == null ? null : MapToResponse(collection);
    }

    public async Task<PagedResult<CollectionResponse>> GetCollectionsByMemberAsync(int memberId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = _unitOfWork.Context.Set<collection>()
            .Include(c => c.venues)
            .Where(c => c.member_id == memberId && c.is_deleted != true);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(c => c.created_at)
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
        var collection = await _unitOfWork.Context.Set<collection>()
            .FirstOrDefaultAsync(c => c.id == collectionId && c.member_id == memberId && c.is_deleted != true, cancellationToken);

        if (collection == null)
            return null;

        if (!string.IsNullOrEmpty(request.CollectionName))
            collection.collection_name = request.CollectionName;
        
        if (request.Description != null)
            collection.description = request.Description;
        
        if (!string.IsNullOrEmpty(request.Status))
            collection.status = request.Status;

        collection.updated_at = DateTime.UtcNow;

        _unitOfWork.Context.Set<collection>().Update(collection);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Updated collection {CollectionId}", collectionId);

        return MapToResponse(collection);
    }

    public async Task<bool> DeleteCollectionAsync(int collectionId, int memberId, CancellationToken cancellationToken = default)
    {
        var collection = await _unitOfWork.Context.Set<collection>()
            .FirstOrDefaultAsync(c => c.id == collectionId && c.member_id == memberId && c.is_deleted != true, cancellationToken);

        if (collection == null)
            return false;

        collection.is_deleted = true;
        collection.updated_at = DateTime.UtcNow;

        _unitOfWork.Context.Set<collection>().Update(collection);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Deleted collection {CollectionId}", collectionId);

        return true;
    }

    public async Task<CollectionResponse?> AddVenuesToCollectionAsync(int collectionId, int memberId, PatchCollectionRequest request, CancellationToken cancellationToken = default)
    {
        var collection = await _unitOfWork.Context.Set<collection>()
            .Include(c => c.venues)
            .FirstOrDefaultAsync(c => c.id == collectionId && c.member_id == memberId && c.is_deleted != true, cancellationToken);

        if (collection == null)
            return null;

        var venuesToAdd = await _unitOfWork.Context.Set<venue_location>()
            .Where(v => request.VenueIds.Contains(v.id))
            .ToListAsync(cancellationToken);

        foreach (var venue in venuesToAdd)
        {
            if (!collection.venues.Any(v => v.id == venue.id))
            {
                collection.venues.Add(venue);
            }
        }

        collection.updated_at = DateTime.UtcNow;
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Added {Count} venues to collection {CollectionId}", venuesToAdd.Count, collectionId);

        return MapToResponse(collection);
    }

    public async Task<CollectionResponse?> RemoveVenuesFromCollectionAsync(int collectionId, int memberId, PatchCollectionRequest request, CancellationToken cancellationToken = default)
    {
        var collection = await _unitOfWork.Context.Set<collection>()
            .Include(c => c.venues)
            .FirstOrDefaultAsync(c => c.id == collectionId && c.member_id == memberId && c.is_deleted != true, cancellationToken);

        if (collection == null)
            return null;

        var venuesToRemove = collection.venues
            .Where(v => request.VenueIds.Contains(v.id))
            .ToList();

        foreach (var venue in venuesToRemove)
        {
            collection.venues.Remove(venue);
        }

        collection.updated_at = DateTime.UtcNow;
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Removed {Count} venues from collection {CollectionId}", venuesToRemove.Count, collectionId);

        return MapToResponse(collection);
    }

    private CollectionResponse MapToResponse(collection collection)
    {
        return new CollectionResponse
        {
            Id = collection.id,
            MemberId = collection.member_id,
            CollectionName = collection.collection_name,
            Description = collection.description,
            Status = collection.status,
            CreatedAt = collection.created_at,
            UpdatedAt = collection.updated_at,
            Venues = collection.venues?.Select(v => new VenueSimpleResponse
            {
                Id = v.id,
                Name = v.name,
                Description = v.description,
                Address = v.address
            }).ToList()
        };
    }
}
