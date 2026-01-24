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

    /// <summary>
    /// Create a new venue location with location tags
    /// </summary>
    public async Task<VenueLocationDetailResponse> CreateVenueLocationAsync(CreateVenueLocationRequest request, int venueOwnerId)
    {
        _logger.LogInformation("Creating new venue location: {VenueName} for owner {VenueOwnerId}", request.Name, venueOwnerId);

        // Create new venue location entity
        var venueLocation = new VenueLocation
        {
            Name = request.Name,
            Description = request.Description,
            Address = request.Address,
            Email = request.Email,
            PhoneNumber = request.PhoneNumber,
            WebsiteUrl = request.WebsiteUrl,
            OpeningTime = request.OpeningTime,
            ClosingTime = request.ClosingTime,
            IsOpen = request.IsOpen ?? false,
            PriceMin = request.PriceMin,
            PriceMax = request.PriceMax,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            VenueOwnerId = venueOwnerId,
            Status = "Active",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsDeleted = false,
            AverageRating = null,
            ReviewCount = 0
        };

        // Set location tag if provided (using first one)
        if (request.LocationTagIds.Any())
        {
            venueLocation.LocationTagId = request.LocationTagIds.First();
        }

        // Add to database
        await _unitOfWork.VenueLocations.AddAsync(venueLocation);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Venue location created successfully with ID {VenueId}", venueLocation.Id);

        // Retrieve with details
        return await GetVenueLocationDetailByIdAsync(venueLocation.Id) 
            ?? throw new InvalidOperationException("Failed to retrieve created venue location");
    }

    /// <summary>
    /// Update venue location information
    /// </summary>
    public async Task<VenueLocationDetailResponse?> UpdateVenueLocationAsync(int id, UpdateVenueLocationRequest request)
    {
        _logger.LogInformation("Updating venue location with ID {VenueId}", id);

        var venue = await _unitOfWork.VenueLocations.GetByIdAsync(id);
        
        if (venue == null || venue.IsDeleted == true)
        {
            _logger.LogWarning("Venue location with ID {VenueId} not found or deleted", id);
            return null;
        }

        // Update properties if provided
        if (!string.IsNullOrEmpty(request.Name))
            venue.Name = request.Name;
        
        if (request.Description != null)
            venue.Description = request.Description;
        
        if (!string.IsNullOrEmpty(request.Address))
            venue.Address = request.Address;
        
        if (request.Email != null)
            venue.Email = request.Email;
        
        if (request.PhoneNumber != null)
            venue.PhoneNumber = request.PhoneNumber;
        
        if (request.WebsiteUrl != null)
            venue.WebsiteUrl = request.WebsiteUrl;
        
        if (request.OpeningTime.HasValue)
            venue.OpeningTime = request.OpeningTime;
        
        if (request.ClosingTime.HasValue)
            venue.ClosingTime = request.ClosingTime;
        
        if (request.IsOpen.HasValue)
            venue.IsOpen = request.IsOpen;
        
        if (request.PriceMin.HasValue)
            venue.PriceMin = request.PriceMin;
        
        if (request.PriceMax.HasValue)
            venue.PriceMax = request.PriceMax;
        
        if (request.Latitude.HasValue)
            venue.Latitude = request.Latitude;
        
        if (request.Longitude.HasValue)
            venue.Longitude = request.Longitude;

        venue.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.VenueLocations.Update(venue);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Venue location {VenueId} updated successfully", id);

        return await GetVenueLocationDetailByIdAsync(id);
    }

    /// <summary>
    /// Get all location tags with couple mood type and couple personality type
    /// </summary>
    public async Task<List<LocationTagResponse>> GetAllLocationTagsAsync()
    {
        _logger.LogInformation("Retrieving all location tags");

        var locationTags = await _unitOfWork.Context.Set<LocationTag>()
            .AsNoTracking()
            .Include(lt => lt.CoupleMoodType)
            .Include(lt => lt.CouplePersonalityType)
            .Where(lt => lt.IsDeleted != true)
            .ToListAsync();

        var responses = locationTags
            .Select(lt => new LocationTagResponse
            {
                Id = lt.Id,
                TagName = GenerateTagName(lt.CoupleMoodType?.Name, lt.CouplePersonalityType?.Name),
                CoupleMoodType = lt.CoupleMoodType != null ? new CoupleMoodTypeInfo
                {
                    Id = lt.CoupleMoodType.Id,
                    Name = lt.CoupleMoodType.Name,
                    Description = lt.CoupleMoodType.Description,
                    IsActive = lt.CoupleMoodType.IsActive
                } : null,
                CouplePersonalityType = lt.CouplePersonalityType != null ? new CouplePersonalityTypeInfo
                {
                    Id = lt.CouplePersonalityType.Id,
                    Name = lt.CouplePersonalityType.Name,
                    Description = lt.CouplePersonalityType.Description,
                    IsActive = lt.CouplePersonalityType.IsActive
                } : null
            })
            .ToList();

        _logger.LogInformation("Retrieved {Count} location tags", responses.Count);
        return responses;
    }

    /// <summary>
    /// Generate tag name by combining couple mood type and couple personality type
    /// </summary>
    /// <param name="moodTypeName">Couple mood type name</param>
    /// <param name="personalityTypeName">Couple personality type name</param>
    /// <returns>Generated tag name in format "mood - personality"</returns>
    private string? GenerateTagName(string? moodTypeName, string? personalityTypeName)
    {
        if (string.IsNullOrEmpty(moodTypeName) && string.IsNullOrEmpty(personalityTypeName))
            return null;

        if (string.IsNullOrEmpty(moodTypeName))
            return personalityTypeName;

        if (string.IsNullOrEmpty(personalityTypeName))
            return moodTypeName;

        return $"{moodTypeName} - {personalityTypeName}";
    }

}
