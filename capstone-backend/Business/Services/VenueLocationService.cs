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
    /// Get venue location detail by ID including location tag, venue owner profile, and opening hours
    /// </summary>
    public async Task<VenueLocationDetailResponse?> GetVenueLocationDetailByIdAsync(int venueId)
    {
        var venue = await _unitOfWork.VenueLocations.GetByIdWithDetailsAsync(venueId);

        if (venue == null)
        {
            _logger.LogWarning("Venue location with ID {VenueId} not found or deleted", venueId);
            return null;
        }

        var response = _mapper.Map<VenueLocationDetailResponse>(venue);

        // Fetch opening hours for the venue
        var openingHours = await _unitOfWork.Context.Set<VenueOpeningHour>()
            .Where(x => x.VenueLocationId == venueId)
            .OrderBy(x => x.Day)
            .ToListAsync();

        if (openingHours.Any())
        {
            response.OpeningHours = openingHours.Select(oh => new VenueOpeningHourResponse
            {
                Id = oh.Id,
                VenueLocationId = oh.VenueLocationId,
                Day = oh.Day,
                OpenTime = oh.OpenTime,
                CloseTime = oh.CloseTime,
                IsClosed = oh.IsClosed
            }).ToList();
        }

        _logger.LogInformation("Retrieved venue location detail for ID {VenueId} with {HourCount} opening hours", venueId, openingHours.Count);

        return response;
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
    public async Task<VenueLocationDetailResponse> CreateVenueLocationAsync(CreateVenueLocationRequest request, int userId)
    {
        _logger.LogInformation("Creating new venue location: {VenueName} for user {UserId}", request.Name, userId);

        // Find VenueOwnerProfile for the user
        var venueOwnerProfile = await _unitOfWork.Context.Set<VenueOwnerProfile>()
            .FirstOrDefaultAsync(vop => vop.UserId == userId && vop.IsDeleted != true);

        if (venueOwnerProfile == null)
        {
            _logger.LogError("User {UserId} does not have a venue owner profile", userId);
            throw new InvalidOperationException($"User {userId} is not registered as a venue owner. Please create a venue owner profile first.");
        }

        _logger.LogInformation("Found venue owner profile ID {VenueOwnerProfileId} for user {UserId}", venueOwnerProfile.Id, userId);

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
            CoverImage = request.CoverImage,
            InteriorImage = request.InteriorImage,
            FullPageMenuImage = request.FullPageMenuImage,
            IsOwnerVerified = request.IsOwnerVerified ?? false,
            VenueOwnerId = venueOwnerProfile.Id,
            Status = "Active",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsDeleted = false,
            AverageRating = null,
            ReviewCount = 0
        };

        // Find and set location tag based on couple mood type and personality type IDs
        if (request.CoupleMoodTypeId.HasValue || request.CouplePersonalityTypeId.HasValue)
        {
            var locationTag = await _unitOfWork.LocationTags.GetByMoodAndPersonalityTypeIdsAsync(
                request.CoupleMoodTypeId, 
                request.CouplePersonalityTypeId);

            if (locationTag != null)
            {
                venueLocation.LocationTagId = locationTag.Id;
                _logger.LogInformation("Found location tag ID {LocationTagId} for couple mood type {CoupleMoodTypeId} and personality type {CouplePersonalityTypeId}",
                    locationTag.Id, request.CoupleMoodTypeId, request.CouplePersonalityTypeId);
            }
            else
            {
                _logger.LogWarning("No location tag found for couple mood type {CoupleMoodTypeId} and personality type {CouplePersonalityTypeId}",
                    request.CoupleMoodTypeId, request.CouplePersonalityTypeId);
            }
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
        
        if (request.CoverImage != null)
            venue.CoverImage = request.CoverImage;
        
        if (request.InteriorImage != null)
            venue.InteriorImage = request.InteriorImage;
        
        if (request.FullPageMenuImage != null)
            venue.FullPageMenuImage = request.FullPageMenuImage;
        
        if (request.IsOwnerVerified.HasValue)
            venue.IsOwnerVerified = request.IsOwnerVerified;

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
    /// Get all couple mood types
    /// </summary>
    public async Task<List<CoupleMoodTypeInfo>> GetAllCoupleMoodTypesAsync()
    {
        _logger.LogInformation("Retrieving all couple mood types");

        var moodTypes = await _unitOfWork.Context.Set<CoupleMoodType>()
            .AsNoTracking()
            .Where(mt => mt.IsDeleted != true && mt.IsActive == true)
            .OrderBy(mt => mt.Name)
            .ToListAsync();

        return moodTypes
            .Select(mt => new CoupleMoodTypeInfo
            {
                Id = mt.Id,
                Name = mt.Name,
                Description = mt.Description,
                IsActive = mt.IsActive
            })
            .ToList();
    }

    /// <summary>
    /// Get all couple personality types
    /// </summary>
    public async Task<List<CouplePersonalityTypeInfo>> GetAllCouplePersonalityTypesAsync()
    {
        _logger.LogInformation("Retrieving all couple personality types");

        var personalityTypes = await _unitOfWork.Context.Set<CouplePersonalityType>()
            .AsNoTracking()
            .Where(pt => pt.IsDeleted != true && pt.IsActive == true)
            .OrderBy(pt => pt.Name)
            .ToListAsync();

        return personalityTypes
            .Select(pt => new CouplePersonalityTypeInfo
            {
                Id = pt.Id,
                Name = pt.Name,
                Description = pt.Description,
                IsActive = pt.IsActive
            })
            .ToList();
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

    /// <summary>
    /// Update venue opening hours for a specific day
    /// Automatically updates is_closed based on current time
    /// </summary>
    public async Task<VenueOpeningHourResponse?> UpdateVenueOpeningHourAsync(UpdateVenueOpeningHourRequest request)
    {
        // Validate day range (2-8)
        if (request.Day < 2 || request.Day > 8)
        {
            _logger.LogWarning("Invalid day value {Day}. Must be between 2-8", request.Day);
            return null;
        }

        // Parse time strings
        if (!TimeSpan.TryParse(request.OpenTime, out var openTime))
        {
            _logger.LogWarning("Invalid open time format: {OpenTime}", request.OpenTime);
            return null;
        }

        if (!TimeSpan.TryParse(request.CloseTime, out var closeTime))
        {
            _logger.LogWarning("Invalid close time format: {CloseTime}", request.CloseTime);
            return null;
        }

        try
        {
            var venueLocationId = request.VenueLocationId;
            var day = request.Day; // Keep as integer
            
            // Query with integer day
            var openingHour = await _unitOfWork.Context.Set<VenueOpeningHour>()
                .Where(x => x.VenueLocationId == venueLocationId && x.Day == day)
                .FirstOrDefaultAsync();

            if (openingHour == null)
            {
                // Create new opening hour record
                openingHour = new VenueOpeningHour
                {
                    VenueLocationId = request.VenueLocationId,
                    Day = request.Day,
                    OpenTime = openTime,
                    CloseTime = closeTime,
                    IsClosed = false
                };

                await _unitOfWork.Context.Set<VenueOpeningHour>().AddAsync(openingHour);
            }
            else
            {
                // Update existing opening hour record
                openingHour.OpenTime = openTime;
                openingHour.CloseTime = closeTime;
                _unitOfWork.Context.Set<VenueOpeningHour>().Update(openingHour);
            }

            // Check current time and auto-update is_closed
            var currentTimeVN = DateTime.UtcNow.AddHours(7); // Vietnam time (UTC+7)
            var currentTime = currentTimeVN.TimeOfDay;
            
         

            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Updated venue opening hour for venue {VenueId}, day {Day}", request.VenueLocationId, request.Day);

            return new VenueOpeningHourResponse
            {
                Id = openingHour.Id,
                VenueLocationId = openingHour.VenueLocationId,
                Day = request.Day,
                OpenTime = openingHour.OpenTime,
                CloseTime = openingHour.CloseTime,
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating venue opening hour for venue {VenueId}", request.VenueLocationId);
            return null;
        }
    }

}
