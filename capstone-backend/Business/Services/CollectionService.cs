using capstone_backend.Business.Common.Constants;
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
        var normalizedCollectionName = NormalizeCollectionName(request.CollectionName);

        var nameExists = await _unitOfWork.Context.Set<Collection>()
            .AnyAsync(c => c.MemberId == memberId
                && c.IsDeleted != true
                && c.CollectionName != null
                && c.CollectionName.Trim().ToLower() == normalizedCollectionName,
                cancellationToken);

        if (nameExists)
        {
            throw new InvalidOperationException($"Collection name '{request.CollectionName}' already exists.");
        }

        var collection = new Collection()
        {
            MemberId = memberId,
            CollectionName = request.CollectionName.Trim(),
            Description = request.Description,
            Img = request.Img,
            Status = request.Status,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsDeleted = false
        };

        await _unitOfWork.Context.Set<Collection>().AddAsync(collection, cancellationToken);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Created collection {CollectionId} for member {MemberId}", collection.Id, memberId);

        return MapToResponse(collection);
    }

    public async Task<CollectionResponse> CreateDefaultCollectionForMemberAsync(int memberId, CancellationToken cancellationToken = default)
    {
        var collection = new Collection()
        {
            MemberId = memberId,
            CollectionName = CollectionConstants.DEFAULT_COLLECTION_NAME,
            Description = CollectionConstants.DEFAULT_COLLECTION_DESCRIPTION,
            Status = CollectionConstants.DEFAULT_COLLECTION_STATUS,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsDeleted = false
        };

        await _unitOfWork.Context.Set<Collection>().AddAsync(collection, cancellationToken);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Created default collection '{CollectionName}' for member {MemberId}", 
            CollectionConstants.DEFAULT_COLLECTION_NAME, memberId);

        return MapToResponse(collection);
    }

    public async Task<CollectionResponse?> GetCollectionByIdAsync(int collectionId, CancellationToken cancellationToken = default)
    {
        var collection = await _unitOfWork.Context.Set<Collection>()
            .Include(c => c.Venues)
                .ThenInclude(v => v.VenueLocationTags)
                    .ThenInclude(vlt => vlt.LocationTag)
                        .ThenInclude(lt => lt.CoupleMoodType)
            .Include(c => c.Venues)
                .ThenInclude(v => v.VenueLocationTags)
                    .ThenInclude(vlt => vlt.LocationTag)
                        .ThenInclude(lt => lt.CouplePersonalityType)
            .FirstOrDefaultAsync(c => c.Id == collectionId && c.IsDeleted != true, cancellationToken);

        return collection == null ? null : MapToResponse(collection);
    }

    public async Task<CollectionResponse> GetCurrentCollectionAsync(int memberId, CancellationToken cancellationToken = default)
    {
        // Tìm collection mặc định của member
        var collection = await _unitOfWork.Context.Set<Collection>()
            .Include(c => c.Venues)
                .ThenInclude(v => v.VenueLocationTags)
                    .ThenInclude(vlt => vlt.LocationTag)
                        .ThenInclude(lt => lt.CoupleMoodType)
            .Include(c => c.Venues)
                .ThenInclude(v => v.VenueLocationTags)
                    .ThenInclude(vlt => vlt.LocationTag)
                        .ThenInclude(lt => lt.CouplePersonalityType)
            .FirstOrDefaultAsync(c => c.MemberId == memberId 
                && c.CollectionName == CollectionConstants.DEFAULT_COLLECTION_NAME 
                && c.IsDeleted != true, cancellationToken);

        // Nếu chưa có thì tạo mới
        if (collection == null)
        {
            _logger.LogInformation("Default collection not found for member {MemberId}, creating new one", memberId);
            return await CreateDefaultCollectionForMemberAsync(memberId, cancellationToken);
        }

        return MapToResponse(collection);
    }

    public async Task<PagedResult<CollectionResponse>> GetCollectionsByMemberAsync(int memberId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = _unitOfWork.Context.Set<Collection>()
            .Include(c => c.Venues)
                .ThenInclude(v => v.VenueLocationTags)
                    .ThenInclude(vlt => vlt.LocationTag)
                        .ThenInclude(lt => lt.CoupleMoodType)
            .Include(c => c.Venues)
                .ThenInclude(v => v.VenueLocationTags)
                    .ThenInclude(vlt => vlt.LocationTag)
                        .ThenInclude(lt => lt.CouplePersonalityType)
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

    public async Task<List<CollectionSummaryResponse>> GetCollectionSummariesByMemberAsync(int memberId, CancellationToken cancellationToken = default)
    {
        var collections = await _unitOfWork.Context.Set<Collection>()
            .Where(c => c.MemberId == memberId && c.IsDeleted != true)
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => new CollectionSummaryResponse
            {
                Id = c.Id,
                CollectionName = c.CollectionName,
                Description = c.Description,
                Img = c.Img,
                Status = c.Status
            })
            .ToListAsync(cancellationToken);

        return collections;
    }

    public async Task<CollectionResponse?> UpdateCollectionAsync(int collectionId, int memberId, UpdateCollectionRequest request, CancellationToken cancellationToken = default)
    {
        var collection = await _unitOfWork.Context.Set<Collection>()
            .FirstOrDefaultAsync(c => c.Id == collectionId && c.MemberId == memberId && c.IsDeleted != true, cancellationToken);

        if (collection == null)
            return null;

        // Bảo vệ collection mặc định khỏi việc đổi tên
        if (collection.CollectionName == CollectionConstants.DEFAULT_COLLECTION_NAME && 
            !string.IsNullOrEmpty(request.CollectionName) && 
            request.CollectionName != CollectionConstants.DEFAULT_COLLECTION_NAME)
        {
            throw new InvalidOperationException("Không thể đổi tên collection mặc định");
        }

        if (!string.IsNullOrWhiteSpace(request.CollectionName))
        {
            var normalizedNewName = NormalizeCollectionName(request.CollectionName);

            var duplicateNameExists = await _unitOfWork.Context.Set<Collection>()
                .AnyAsync(c => c.MemberId == memberId
                    && c.Id != collectionId
                    && c.IsDeleted != true
                    && c.CollectionName != null
                    && c.CollectionName.Trim().ToLower() == normalizedNewName,
                    cancellationToken);

            if (duplicateNameExists)
            {
                throw new InvalidOperationException($"Collection name '{request.CollectionName}' already exists.");
            }

            collection.CollectionName = request.CollectionName.Trim();
        }
        
        if (request.Description != null)
            collection.Description = request.Description;
        
        if (request.Img != null)
            collection.Img = request.Img;
        
        if (!string.IsNullOrEmpty(request.Status))
            collection.Status = request.Status;

        collection.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.Context.Set<Collection>().Update(collection);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Updated collection {CollectionId}", collectionId);

        return MapToResponse(collection);
    }

    public async Task<CollectionResponse?> UpdateCollectionStatusAsync(int collectionId, int memberId, UpdateCollectionStatusRequest request, CancellationToken cancellationToken = default)
    {
        var collection = await _unitOfWork.Context.Set<Collection>()
            .FirstOrDefaultAsync(c => c.Id == collectionId && c.MemberId == memberId && c.IsDeleted != true, cancellationToken);

        if (collection == null)
            return null;

        // Bảo vệ collection mặc định khỏi việc đổi status
        if (collection.CollectionName == CollectionConstants.DEFAULT_COLLECTION_NAME)
        {
            throw new InvalidOperationException("Không thể thay đổi trạng thái của collection mặc định");
        }

        collection.Status = request.Status;
        collection.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.Context.Set<Collection>().Update(collection);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Updated status of collection {CollectionId} to {Status}", collectionId, request.Status);

        return MapToResponse(collection);
    }

    public async Task<bool> DeleteCollectionAsync(int collectionId, int memberId, CancellationToken cancellationToken = default)
    {
        var collection = await _unitOfWork.Context.Set<Collection>()
            .FirstOrDefaultAsync(c => c.Id == collectionId && c.MemberId == memberId && c.IsDeleted != true, cancellationToken);

        if (collection == null)
            return false;

        // Bảo vệ collection mặc định khỏi việc xóa
        if (collection.CollectionName == CollectionConstants.DEFAULT_COLLECTION_NAME)
        {
            throw new InvalidOperationException("Không thể xóa collection mặc định");
        }

        collection.IsDeleted = true;
        collection.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.Context.Set<Collection>().Update(collection);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Deleted collection {CollectionId}", collectionId);

        return true;
    }

    public async Task<CollectionResponse?> AddVenueToCollectionAsync(int collectionId, int memberId, int venueId, CancellationToken cancellationToken = default)
    {
        var collection = await _unitOfWork.Context.Set<Collection>()
            .Include(c => c.Venues)
            .FirstOrDefaultAsync(c => c.Id == collectionId && c.MemberId == memberId && c.IsDeleted != true, cancellationToken);

        if (collection == null)
            return null;

        var venue = await _unitOfWork.Context.Set<VenueLocation>()
            .FirstOrDefaultAsync(v => v.Id == venueId, cancellationToken);

        if (venue == null)
            return null;

        if (!collection.Venues.Any(v => v.Id == venueId))
        {
            collection.Venues.Add(venue);
            collection.UpdatedAt = DateTime.UtcNow;
            
            // Chỉ tăng FavoriteCount khi add vào collection mặc định (Mục yêu thích)
            if (collection.CollectionName == CollectionConstants.DEFAULT_COLLECTION_NAME)
            {
                venue.FavoriteCount = (venue.FavoriteCount ?? 0) + 1;
            }
            
            await _unitOfWork.SaveChangesAsync();
            
        }

        collection = await _unitOfWork.Context.Set<Collection>()
            .Include(c => c.Venues)
                .ThenInclude(v => v.VenueLocationTags)
                    .ThenInclude(vlt => vlt.LocationTag)
                        .ThenInclude(lt => lt.CoupleMoodType)
            .Include(c => c.Venues)
                .ThenInclude(v => v.VenueLocationTags)
                    .ThenInclude(vlt => vlt.LocationTag)
                        .ThenInclude(lt => lt.CouplePersonalityType)
            .FirstOrDefaultAsync(c => c.Id == collectionId, cancellationToken);

        return MapToResponse(collection!);
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

        // Chỉ tăng FavoriteCount khi add vào collection mặc định
        bool isDefaultCollection = collection.CollectionName == CollectionConstants.DEFAULT_COLLECTION_NAME;

        foreach (var venue in venuesToAdd)
        {
            if (!collection.Venues.Any(v => v.Id == venue.Id))
            {
                collection.Venues.Add(venue);
                
                if (isDefaultCollection)
                {
                    venue.FavoriteCount = (venue.FavoriteCount ?? 0) + 1;
                }
            }
        }

        collection.UpdatedAt = DateTime.UtcNow;
        await _unitOfWork.SaveChangesAsync();

        collection = await _unitOfWork.Context.Set<Collection>()
            .Include(c => c.Venues)
                .ThenInclude(v => v.VenueLocationTags)
                    .ThenInclude(vlt => vlt.LocationTag)
                        .ThenInclude(lt => lt.CoupleMoodType)
            .Include(c => c.Venues)
                .ThenInclude(v => v.VenueLocationTags)
                    .ThenInclude(vlt => vlt.LocationTag)
                        .ThenInclude(lt => lt.CouplePersonalityType)
            .FirstOrDefaultAsync(c => c.Id == collectionId, cancellationToken);

        return MapToResponse(collection!);
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

        // Chỉ giảm FavoriteCount khi remove khỏi collection mặc định
        bool isDefaultCollection = collection.CollectionName == CollectionConstants.DEFAULT_COLLECTION_NAME;

        foreach (var venue in venuesToRemove)
        {
            collection.Venues.Remove(venue);
            
            if (isDefaultCollection && venue.FavoriteCount > 0)
            {
                venue.FavoriteCount = venue.FavoriteCount - 1;
            }
        }

        collection.UpdatedAt = DateTime.UtcNow;
        await _unitOfWork.SaveChangesAsync();

        collection = await _unitOfWork.Context.Set<Collection>()
            .Include(c => c.Venues)
                .ThenInclude(v => v.VenueLocationTags)
                    .ThenInclude(vlt => vlt.LocationTag)
                        .ThenInclude(lt => lt.CoupleMoodType)
            .Include(c => c.Venues)
                .ThenInclude(v => v.VenueLocationTags)
                    .ThenInclude(vlt => vlt.LocationTag)
                        .ThenInclude(lt => lt.CouplePersonalityType)
            .FirstOrDefaultAsync(c => c.Id == collectionId, cancellationToken);

        return MapToResponse(collection!);
    }

    private CollectionResponse MapToResponse(Collection collection)
    {
        return new CollectionResponse
        {
            Id = collection.Id,
            MemberId = collection.MemberId,
            CollectionName = collection.CollectionName,
            Description = collection.Description,
            Img = collection.Img,
            Status = collection.Status,
            CreatedAt = collection.CreatedAt,
            UpdatedAt = collection.UpdatedAt,
            Venues = collection.Venues?.Select(v => new VenueSimpleResponse
            {
                Id = v.Id,
                Name = v.Name,
                Description = v.Description,
                Address = v.Address,
                CoverImage = v.CoverImage,
                InteriorImage = ParseImageToList(v.InteriorImage),
                CoupleMoodTypes = v.VenueLocationTags
                    ?.Where(vlt => vlt.IsDeleted != true && vlt.LocationTag?.CoupleMoodType != null)
                    .Select(vlt => new DTOs.VenueLocation.CoupleMoodTypeInfo
                    {
                        Id = vlt.LocationTag!.CoupleMoodType!.Id,
                        Name = vlt.LocationTag.CoupleMoodType.Name
                    })
                    .DistinctBy(m => m.Id)
                    .ToList(),
                CouplePersonalityTypes = v.VenueLocationTags
                    ?.Where(vlt => vlt.IsDeleted != true && vlt.LocationTag?.CouplePersonalityType != null)
                    .Select(vlt => new DTOs.VenueLocation.CouplePersonalityTypeInfo
                    {
                        Id = vlt.LocationTag!.CouplePersonalityType!.Id,
                        Name = vlt.LocationTag.CouplePersonalityType.Name
                    })
                    .DistinctBy(p => p.Id)
                    .ToList()
            }).ToList()
        };
    }

    private static List<string> ParseImageToList(string? imageField)
    {
        if (string.IsNullOrWhiteSpace(imageField))
            return new List<string>();

        var trimmed = imageField.Trim();

        while ((trimmed.StartsWith("'") && trimmed.EndsWith("'")) ||
               (trimmed.StartsWith("\"") && trimmed.EndsWith("\"")))
        {
            trimmed = trimmed.Substring(1, trimmed.Length - 2).Trim();
        }

        if (trimmed.StartsWith("[") && trimmed.EndsWith("]"))
        {
            try
            {
                var unescaped = trimmed.Replace("\\\"", "\"");
                var parsed = System.Text.Json.JsonSerializer.Deserialize<List<string>>(unescaped);
                if (parsed != null && parsed.Any())
                {
                    return parsed.Where(s => !string.IsNullOrWhiteSpace(s)).ToList();
                }
            }
            catch
            {
            }
        }

        return new List<string> { trimmed };
    }

    private static string NormalizeCollectionName(string collectionName)
    {
        if (string.IsNullOrWhiteSpace(collectionName))
        {
            throw new InvalidOperationException("Collection name is required.");
        }

        return collectionName.Trim().ToLower();
    }
}
