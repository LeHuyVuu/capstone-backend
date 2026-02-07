using capstone_backend.Business.DTOs.Collection;
using capstone_backend.Business.DTOs.Common;

namespace capstone_backend.Business.Interfaces;

public interface ICollectionService
{
    Task<CollectionResponse> CreateCollectionAsync(int memberId, CreateCollectionRequest request, CancellationToken cancellationToken = default);
    Task<CollectionResponse> CreateDefaultCollectionForMemberAsync(int memberId, CancellationToken cancellationToken = default);
    Task<CollectionResponse?> GetCollectionByIdAsync(int collectionId, CancellationToken cancellationToken = default);
    Task<CollectionResponse> GetCurrentCollectionAsync(int memberId, CancellationToken cancellationToken = default);
    Task<PagedResult<CollectionResponse>> GetCollectionsByMemberAsync(int memberId, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<CollectionResponse?> UpdateCollectionAsync(int collectionId, int memberId, UpdateCollectionRequest request, CancellationToken cancellationToken = default);
    Task<CollectionResponse?> UpdateCollectionStatusAsync(int collectionId, int memberId, UpdateCollectionStatusRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteCollectionAsync(int collectionId, int memberId, CancellationToken cancellationToken = default);
    Task<CollectionResponse?> AddVenueToCollectionAsync(int collectionId, int memberId, int venueId, CancellationToken cancellationToken = default);
    Task<CollectionResponse?> AddVenuesToCollectionAsync(int collectionId, int memberId, PatchCollectionRequest request, CancellationToken cancellationToken = default);
    Task<CollectionResponse?> RemoveVenuesFromCollectionAsync(int collectionId, int memberId, PatchCollectionRequest request, CancellationToken cancellationToken = default);
}
