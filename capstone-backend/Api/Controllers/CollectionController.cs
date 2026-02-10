using capstone_backend.Business.DTOs.Collection;
using capstone_backend.Business.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace capstone_backend.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class CollectionController : BaseController
{
    private readonly ICollectionService _collectionService;
    private readonly IUnitOfWork _unitOfWork;

    public CollectionController(ICollectionService collectionService, IUnitOfWork unitOfWork)
    {
        _collectionService = collectionService;
        _unitOfWork = unitOfWork;
    }

    private async Task<int> GetCurrentMemberIdAsync()
    {
        // Get UserId from JWT token
        var userIdClaim = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value 
                         ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
        {
            throw new UnauthorizedAccessException("User ID not found in token");
        }

        // Query MemberId from database using UserId
        var memberProfile = await _unitOfWork.MembersProfile.GetByUserIdAsync(userId);
        if (memberProfile == null)
        {
            throw new UnauthorizedAccessException("Member profile not found for this user");
        }

        return memberProfile.Id;
    }

    /// <summary>
    /// Create a new collection for the current member. Status is PRIVATE or PUBLIC.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateCollection([FromBody] CreateCollectionRequest request)
    {
        var memberId = await GetCurrentMemberIdAsync();
        var collection = await _collectionService.CreateCollectionAsync(memberId, request);
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
    /// Get current collection (default collection) for current member
    /// </summary>
    [HttpGet("current")]
    public async Task<IActionResult> GetCurrentCollection()
    {
        var memberId = await GetCurrentMemberIdAsync();
        var collection = await _collectionService.GetCurrentCollectionAsync(memberId);
        return OkResponse(collection);
    }

    /// <summary>
    /// Get all collections for current member (paginated)
    /// </summary>
    [HttpGet("my-collections")]
    public async Task<IActionResult> GetMyCollections([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var memberId = await GetCurrentMemberIdAsync();
        var collections = await _collectionService.GetCollectionsByMemberAsync(memberId, page, pageSize);
        return OkResponse(collections);
    }

    /// <summary>
    /// Update collection information
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateCollection(int id, [FromBody] UpdateCollectionRequest request)
    {
        var memberId = await GetCurrentMemberIdAsync();
        var collection = await _collectionService.UpdateCollectionAsync(id, memberId, request);
        if (collection == null)
            return NotFoundResponse("Collection not found or you don't have permission to update it");

        return OkResponse(collection, "Collection updated successfully");
    }

    /// <summary>
    /// Update collection status (PUBLIC or PRIVATE)
    /// </summary>
    [HttpPatch("{id}/status")]
    public async Task<IActionResult> UpdateCollectionStatus(int id, [FromBody] UpdateCollectionStatusRequest request)
    {
        var memberId = await GetCurrentMemberIdAsync();
        var collection = await _collectionService.UpdateCollectionStatusAsync(id, memberId, request);
        if (collection == null)
            return NotFoundResponse("Collection not found or you don't have permission to update it");

        return OkResponse(collection, "Collection status updated successfully");
    }

    /// <summary>
    /// Delete collection (soft delete)
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteCollection(int id)
    {
        var memberId = await GetCurrentMemberIdAsync();
        var result = await _collectionService.DeleteCollectionAsync(id, memberId);
        if (!result)
            return NotFoundResponse("Collection not found or you don't have permission to delete it");

        return OkResponse<object?>(null, "Collection deleted successfully");
    }

    /// <summary>
    /// Add a single venue to collection (no body required)
    /// </summary>
    [HttpPost("{id}/venue/{venueId}")]
    public async Task<IActionResult> AddVenueToCollection(int id, int venueId)
    {
        var memberId = await GetCurrentMemberIdAsync();
        var collection = await _collectionService.AddVenueToCollectionAsync(id, memberId, venueId);
        if (collection == null)
            return NotFoundResponse("Collection or venue not found, or you don't have permission to modify it");

        return OkResponse(collection, "Venue added to collection successfully");
    }

    /// <summary>
    /// Add multiple venues to collection
    /// </summary>
    [HttpPatch("{id}/add-venues")]
    public async Task<IActionResult> AddVenuesToCollection(int id, [FromBody] PatchCollectionRequest request)
    {
        var memberId = await GetCurrentMemberIdAsync();
        var collection = await _collectionService.AddVenuesToCollectionAsync(id, memberId, request);
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
        var memberId = await GetCurrentMemberIdAsync();
        var collection = await _collectionService.RemoveVenuesFromCollectionAsync(id, memberId, request);
        if (collection == null)
            return NotFoundResponse("Collection not found or you don't have permission to modify it");

        return OkResponse(collection, "Venues removed from collection successfully");
    }
}
