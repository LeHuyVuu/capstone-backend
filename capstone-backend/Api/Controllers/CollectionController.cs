using capstone_backend.Business.DTOs.Collection;
using capstone_backend.Business.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace capstone_backend.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class CollectionController : BaseController
{
    private readonly ICollectionService _collectionService;

    public CollectionController(ICollectionService collectionService)
    {
        _collectionService = collectionService;
    }

    /// <summary>
    /// Create a new collection for the current member
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateCollection([FromBody] CreateCollectionRequest request)
    {
        var memberId = GetCurrentUserId();
        if (memberId == null)
            return UnauthorizedResponse();

        var collection = await _collectionService.CreateCollectionAsync(memberId.Value, request);
        return CreatedResponse(collection, "Collection created successfully");
    }

    /// <summary>
    /// Get collection by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetCollectionById(int id)
    {
        var collection = await _collectionService.GetCollectionByIdAsync(id);
        if (collection == null)
            return NotFoundResponse("Collection not found");

        return OkResponse(collection);
    }

    /// <summary>
    /// Get all collections for current member (paginated)
    /// </summary>
    [HttpGet("my-collections")]
    public async Task<IActionResult> GetMyCollections([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var memberId = GetCurrentUserId();
        if (memberId == null)
            return UnauthorizedResponse();

        var collections = await _collectionService.GetCollectionsByMemberAsync(memberId.Value, page, pageSize);
        return OkResponse(collections);
    }

    /// <summary>
    /// Update collection information
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateCollection(int id, [FromBody] UpdateCollectionRequest request)
    {
        var memberId = GetCurrentUserId();
        if (memberId == null)
            return UnauthorizedResponse();

        var collection = await _collectionService.UpdateCollectionAsync(id, memberId.Value, request);
        if (collection == null)
            return NotFoundResponse("Collection not found or you don't have permission to update it");

        return OkResponse(collection, "Collection updated successfully");
    }

    /// <summary>
    /// Delete collection (soft delete)
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteCollection(int id)
    {
        var memberId = GetCurrentUserId();
        if (memberId == null)
            return UnauthorizedResponse();

        var result = await _collectionService.DeleteCollectionAsync(id, memberId.Value);
        if (!result)
            return NotFoundResponse("Collection not found or you don't have permission to delete it");

        return OkResponse<object?>(null, "Collection deleted successfully");
    }

    /// <summary>
    /// Add venues to collection
    /// </summary>
    [HttpPatch("{id}/add-venues")]
    public async Task<IActionResult> AddVenuesToCollection(int id, [FromBody] PatchCollectionRequest request)
    {
        var memberId = GetCurrentUserId();
        if (memberId == null)
            return UnauthorizedResponse();

        var collection = await _collectionService.AddVenuesToCollectionAsync(id, memberId.Value, request);
        if (collection == null)
            return NotFoundResponse("Collection not found or you don't have permission to modify it");

        return OkResponse(collection, "Venues added to collection successfully");
    }

    /// <summary>
    /// Remove venues from collection
    /// </summary>
    [HttpPatch("{id}/remove-venues")]
    public async Task<IActionResult> RemoveVenuesFromCollection(int id, [FromBody] PatchCollectionRequest request)
    {
        var memberId = GetCurrentUserId();
        if (memberId == null)
            return UnauthorizedResponse();

        var collection = await _collectionService.RemoveVenuesFromCollectionAsync(id, memberId.Value, request);
        if (collection == null)
            return NotFoundResponse("Collection not found or you don't have permission to modify it");

        return OkResponse(collection, "Venues removed from collection successfully");
    }
}
