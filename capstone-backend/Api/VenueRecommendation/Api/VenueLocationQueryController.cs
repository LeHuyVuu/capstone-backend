using capstone_backend.Api.Controllers;
using capstone_backend.Api.Models;
using capstone_backend.Api.VenueRecommendation.Api.DTOs;
using capstone_backend.Api.VenueRecommendation.Service;
using capstone_backend.Business.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace capstone_backend.Api.VenueRecommendation.Api;

/// <summary>
/// Controller for venue location search using Meilisearch
/// </summary>
[Route("api/venue-location")]
[ApiController]
public class VenueLocationQueryController : BaseController
{
    private readonly IMeilisearchService _meilisearchService;
    private readonly ILogger<VenueLocationQueryController> _logger;

    private readonly IUnitOfWork _unitOfWork;

    public VenueLocationQueryController(
        IMeilisearchService meilisearchService,
        IUnitOfWork unitOfWork,
        ILogger<VenueLocationQueryController> logger
    )
    {
        _unitOfWork = unitOfWork;
        _meilisearchService = meilisearchService;
        _logger = logger;
    }

    [HttpPost("search")]
    [ProducesResponseType(typeof(ApiResponse<VenueLocationQueryResponse>), 200)]
    public async Task<IActionResult> SearchVenueLocations([FromBody] VenueLocationQueryRequest request)
    {
          var userId = GetCurrentUserId();
          string? coupleMoodTypeName = null;
          string? memberMoodTypeName = null;
          string? couplePersonalityTypeName = null;
          string? memberMbtiType = null;
          int? memberId = null;
          
            if (userId != null)
            {
                var member = await _unitOfWork.MembersProfile.GetByUserIdAsync(userId.Value);
                if (member != null)
                {
                    memberId = member.Id;
                    
                    // Kiểm tra xem member có couple đang ACTIVE không
                    var couple = await _unitOfWork.CoupleProfiles.GetActiveCoupleByMemberIdAsync(member.Id);
                    
                    // Nếu có couple và đang active
                    if (couple != null && string.Equals(couple.Status, "ACTIVE", StringComparison.OrdinalIgnoreCase))
                    {
                        // Lấy couple mood type name từ couple profile
                        if (couple.CoupleMoodTypeId.HasValue)
                        {
                            var coupleMoodType = await _unitOfWork.Context.CoupleMoodTypes
                                .FirstOrDefaultAsync(x => x.Id == couple.CoupleMoodTypeId.Value);
                            coupleMoodTypeName = coupleMoodType?.Name;
                        }
                        
                        // Lấy couple personality type name từ couple profile
                        if (couple.CouplePersonalityTypeId.HasValue)
                        {
                            var couplePersonalityType = await _unitOfWork.Context.CouplePersonalityTypes
                                .FirstOrDefaultAsync(x => x.Id == couple.CouplePersonalityTypeId.Value);
                            couplePersonalityTypeName = couplePersonalityType?.Name;
                        }
                    }
                    else
                    {
                        // Nếu không có couple hoặc không active, lấy individual mood từ member profile
                        if (member.MoodTypesId.HasValue)
                        {
                            var memberMoodType = await _unitOfWork.MoodTypes.GetByIdAsync(member.MoodTypesId.Value);
                            memberMoodTypeName = memberMoodType?.Name;
                        }
                        
                        // Lấy personality test cho member
                        var personality = await _unitOfWork.PersonalityTests.GetCurrentPersonalityAsync(member.Id);
                        memberMbtiType = personality?.ResultCode;
                    }
                }
            }

       _logger.LogError(
            "[VenueLocationQuery] Request fields - Query: '{Query}', Page: {Page}, PageSize: {PageSize}, " +
            "Category: '{Category}', Area: '{Area}', Lat: {Lat}, Lng: {Lng}, RadiusKm: {RadiusKm}, " +
            "MinRating: {MinRating}, MaxRating: {MaxRating}, " +
            "MinPrice: {MinPrice}, MaxPrice: {MaxPrice}, OnlyOpenNow: {OnlyOpenNow}, " +
            "SortBy: '{SortBy}', SortDirection: '{SortDirection}', " +
            "CoupleMoodType: '{CoupleMoodType}', MemberMoodType: '{MemberMoodType}', " +
            "CouplePersonalityType: '{CouplePersonalityType}', MemberMbtiType: '{MemberMbtiType}', " +
            "UserId: {UserId}, MemberId: {MemberId}",
            request.Query, request.Page, request.PageSize,
            request.Category, request.Area, request.Latitude, request.Longitude, request.RadiusKm,
            request.MinRating, request.MaxRating,
            request.MinPrice, request.MaxPrice, request.OnlyOpenNow,
            request.SortBy, request.SortDirection,
            coupleMoodTypeName, memberMoodTypeName,
            couplePersonalityTypeName, memberMbtiType, userId, memberId);
        
        var result = await _meilisearchService.SearchVenueLocationsAsync(request, coupleMoodTypeName, memberMoodTypeName, couplePersonalityTypeName, memberMbtiType, memberId);

        return OkResponse(result, $"Found {result.Recommendations.TotalCount} venues in {result.ProcessingTimeMs}ms");
    }
}
