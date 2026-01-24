using AutoMapper;
using capstone_backend.Business.DTOs.Common;
using capstone_backend.Business.DTOs.User;
using capstone_backend.Business.DTOs.VenueLocation;
using capstone_backend.Business.Interfaces;
using capstone_backend.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace capstone_backend.Business.Services;

/// <summary>
/// Service for venue location operations
/// </summary>
public class VenueLocationService : IVenueLocationService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<VenueLocationService> _logger;

    public VenueLocationService(IUnitOfWork unitOfWork, IMapper mapper, ILogger<VenueLocationService> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    /// <summary>
    /// Get venue location detail by ID including location tag and venue owner profile
    /// </summary>
    public async Task<VenueLocationDetailResponse?> GetVenueLocationDetailByIdAsync(int venueId)
    {
        var venue = await _unitOfWork.VenueLocations.GetByIdWithDetailsAsync(venueId);

        if (venue == null)
        {
            _logger.LogWarning("Venue location with ID {VenueId} not found or deleted", venueId);
            return null;
        }

        _logger.LogInformation("Retrieved venue location detail for ID {VenueId}", venueId);

        return _mapper.Map<VenueLocationDetailResponse>(venue);
    }

    /// <summary>
    /// Get reviews for a venue location with pagination
    /// </summary>
    public async Task<PagedResult<VenueReviewResponse>> GetReviewsByVenueIdAsync(int venueId, int page = 1, int pageSize = 10)
    {
        var (reviews, totalCount) = await _unitOfWork.VenueLocations.GetReviewsByVenueIdAsync(venueId, page, pageSize);

        var reviewResponses = reviews.Select(r => 
        {
            var response = _mapper.Map<VenueReviewResponse>(r);
            
            // Map member information
            if (r.Member != null)
            {
                response.Member = new ReviewMemberInfo
                {
                    Id = r.Member.Id,
                    UserId = r.Member.UserId,
                    FullName = r.Member.FullName,
                    Gender = r.Member.Gender,
                    Bio = r.Member.Bio,
                    DisplayName = r.Member.User?.DisplayName,
                    AvatarUrl = r.Member.User?.AvatarUrl,
                    Email = r.Member.User?.Email
                };
            }

            return response;
        }).ToList();

        _logger.LogInformation("Retrieved {Count} reviews for venue {VenueId} (Page {Page}/{TotalPages})", 
            reviewResponses.Count, venueId, page, Math.Ceiling(totalCount / (double)pageSize));

        return new PagedResult<VenueReviewResponse>
        {
            Items = reviewResponses,
            PageNumber = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

}
