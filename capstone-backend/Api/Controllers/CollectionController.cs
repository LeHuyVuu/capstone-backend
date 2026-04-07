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
            throw new UnauthorizedAccessException("Không tìm thấy ID người dùng trong token");
        }

        // Query MemberId from database using UserId
        var memberProfile = await _unitOfWork.MembersProfile.GetByUserIdAsync(userId);
        if (memberProfile == null)
        {
            throw new UnauthorizedAccessException("Không tìm thấy hồ sơ thành viên của người dùng này");
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
        return CreatedResponse(collection, "Tạo bộ sưu tập thành công");
    }

    /// <summary>
    /// Get collection by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetCollectionById(int id)
    {
        var collection = await _collectionService.GetCollectionByIdAsync(id);
        if (collection == null)
            return NotFoundResponse("Không tìm thấy bộ sưu tập");

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
    /// Get collection summaries (basic info only: name, thumbnail, description) for current member
    /// </summary>
    [HttpGet("summaries")]
    public async Task<IActionResult> GetCollectionSummaries()
    {
        var memberId = await GetCurrentMemberIdAsync();
        var summaries = await _collectionService.GetCollectionSummariesByMemberAsync(memberId);
        return OkResponse(summaries);
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
            return NotFoundResponse("Không tìm thấy bộ sưu tập hoặc bạn không có quyền cập nhật");

        return OkResponse(collection, "Cập nhật bộ sưu tập thành công");
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
            return NotFoundResponse("Không tìm thấy bộ sưu tập hoặc bạn không có quyền cập nhật");

        return OkResponse(collection, "Cập nhật trạng thái bộ sưu tập thành công");
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
            return NotFoundResponse("Không tìm thấy bộ sưu tập hoặc bạn không có quyền xóa");

        return OkResponse<object?>(null, "Xóa bộ sưu tập thành công");
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
            return NotFoundResponse("Không tìm thấy bộ sưu tập hoặc địa điểm, hoặc bạn không có quyền chỉnh sửa");

        return OkResponse(collection, "Thêm địa điểm vào bộ sưu tập thành công");
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
            return NotFoundResponse("Không tìm thấy bộ sưu tập hoặc bạn không có quyền chỉnh sửa");

        return OkResponse(collection, "Thêm nhiều địa điểm vào bộ sưu tập thành công");
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
            return NotFoundResponse("Không tìm thấy bộ sưu tập hoặc bạn không có quyền chỉnh sửa");

        return OkResponse(collection, "Xóa địa điểm khỏi bộ sưu tập thành công");
    }

    /// <summary>
    /// Get share link for a collection (non authenticated)
    /// </summary>
    [AllowAnonymous]
    [HttpGet("{collectionId:int}/share-link")]
    public async Task<IActionResult> GetShareLink([FromRoute] int collectionId)
    {
        try
        {
            var result = await _collectionService.GetCollectionShareLinkAsync(collectionId);
            return OkResponse(result, "Tạo link chia sẻ thành công");
        }
        catch (Exception ex)
        {
            return BadRequestResponse(ex.Message);
        }
    }

    /// <summary>
    /// Get collection details by share link (non authenticated)
    /// </summary>
    [AllowAnonymous]
    [HttpGet("share/{shareCode}")]
    public async Task<IActionResult> GetCollectionByShareLink([FromRoute] string shareCode)
    {
        try
        {
            var result = await _collectionService.GetCollectionByShareLinkAsync(shareCode);
            if (result == null)
                return NotFoundResponse("Collection không tồn tại hoặc đã bị ẩn");
            return OkResponse(result);
        }
        catch (Exception ex)
        {
            return BadRequestResponse(ex.Message);
        }
    }
}
