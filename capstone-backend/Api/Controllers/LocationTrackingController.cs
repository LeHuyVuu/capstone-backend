using capstone_backend.Api.Models;
using capstone_backend.Business.DTOs.LocationTracking;
using capstone_backend.Business.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace capstone_backend.Api.Controllers;

/// <summary>
/// API đơn giản quản lý watchlist theo dõi vị trí
/// Backend chỉ quản lý relationships, Firebase Realtime Database do Flutter tự xử lý
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class LocationTrackingController : BaseController
{
    private readonly ILocationFollowerService _service;

    public LocationTrackingController(ILocationFollowerService service)
    {
        _service = service;
    }

    /// <summary>
    /// Thêm user vào watchlist (bắt đầu theo dõi vị trí)
    /// </summary>
    [HttpPost("watchlist/add")]
    [Authorize]
    public async Task<IActionResult> AddToWatchlist([FromBody] WatchlistRequest request)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return UnauthorizedResponse();

        if (request.TargetUserId == userId.Value)
            return BadRequestResponse("Không thể thêm chính mình vào watchlist");

        var result = await _service.AddToWatchlistAsync(userId.Value, request.TargetUserId);
        
        return result 
            ? OkResponse(true, "Đã thêm vào watchlist")
            : BadRequestResponse("Thêm vào watchlist thất bại");
    }

    /// <summary>
    /// Xóa user khỏi watchlist (ngưng theo dõi)
    /// </summary>
    [HttpPost("watchlist/remove")]
    [Authorize]
    public async Task<IActionResult> RemoveFromWatchlist([FromBody] WatchlistRequest request)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return UnauthorizedResponse();

        var result = await _service.RemoveFromWatchlistAsync(userId.Value, request.TargetUserId);
        
        return result 
            ? OkResponse(true, "Đã xóa khỏi watchlist")
            : BadRequestResponse("Xóa khỏi watchlist thất bại");
    }

    /// <summary>
    /// Lấy danh sách người mình đang theo dõi
    /// </summary>
    [HttpGet("watchlist")]
    [Authorize]
    public async Task<IActionResult> GetWatchlist()
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return UnauthorizedResponse();

        var watchlist = await _service.GetMyWatchlistAsync(userId.Value);
        return OkResponse(watchlist);
    }

    /// <summary>
    /// Lấy danh sách người đang theo dõi mình
    /// </summary>
    [HttpGet("followers")]
    [Authorize]
    public async Task<IActionResult> GetFollowers()
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return UnauthorizedResponse();

        var followers = await _service.GetMyFollowersAsync(userId.Value);
        return OkResponse(followers);
    }
}
