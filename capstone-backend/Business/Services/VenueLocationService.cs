using AutoMapper;
using capstone_backend.Business.DTOs.Common;
using capstone_backend.Business.DTOs.User;
using capstone_backend.Business.DTOs.VenueLocation;
using capstone_backend.Business.Interfaces;
using capstone_backend.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

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

    #region Image JSON Helpers
    
    /// <summary>
    /// Serialize list of image URLs to JSON string (max 5 images)
    /// </summary>
    private static string? SerializeImages(List<string>? images)
    {
        if (images == null || images.Count == 0)
            return null;
        return JsonSerializer.Serialize(images.Take(5).ToList());
    }

    /// <summary>
    /// Deserialize JSON string to list of image URLs
    /// </summary>
    private static List<string>? DeserializeImages(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;
        try
        {
            if (json.TrimStart().StartsWith("["))
                return JsonSerializer.Deserialize<List<string>>(json);
            return new List<string> { json };
        }
        catch (JsonException)
        {
            return new List<string> { json };
        }
    }
    
    #endregion


    /// <summary>
    /// Get venue location detail by ID including location tag, venue owner profile, and today's opening hours
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

        // Deserialize image JSON strings to arrays
        response.CoverImage = DeserializeImages(venue.CoverImage);
        response.InteriorImage = DeserializeImages(venue.InteriorImage);
        response.FullPageMenuImage = DeserializeImages(venue.FullPageMenuImage);

        // Add today's opening hour info
        var todayOpeningHour = venue.VenueOpeningHours?.FirstOrDefault();
        if (todayOpeningHour != null)
        {
            var currentTimeVN = DateTime.UtcNow.AddHours(7);
            var currentTime = currentTimeVN.TimeOfDay;

            response.TodayDayName = GetDayName(currentTimeVN.DayOfWeek);
            response.TodayOpeningHour = _mapper.Map<TodayOpeningHourResponse>(todayOpeningHour);
            response.TodayOpeningHour.Status = GetVenueStatus(todayOpeningHour, currentTime);
        }

        _logger.LogInformation("Retrieved venue location detail for ID {VenueId}", venueId);

        return response;
    }

    /// <summary>
    /// Get venue open/close status (Đang mở cửa / Sắp mở cửa / Đã đóng cửa)
    /// </summary>
    private string GetVenueStatus(VenueOpeningHour oh, TimeSpan currentTime)
    {
        if (oh.IsClosed)
            return "Đã đóng cửa";
        
        if (currentTime < oh.OpenTime)
            return "Sắp mở cửa";
        
        if (currentTime >= oh.CloseTime)
            return "Đã đóng cửa";
        
        return "Đang mở cửa";
    }

    /// <summary>
    /// Get day name in Vietnamese
    /// </summary>
    private string GetDayName(DayOfWeek dayOfWeek) => dayOfWeek switch
    {
        DayOfWeek.Sunday => "Chủ nhật",
        DayOfWeek.Monday => "Thứ 2",
        DayOfWeek.Tuesday => "Thứ 3",
        DayOfWeek.Wednesday => "Thứ 4",
        DayOfWeek.Thursday => "Thứ 5",
        DayOfWeek.Friday => "Thứ 6",
        DayOfWeek.Saturday => "Thứ 7",
        _ => "Unknown"
    };

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
    public async Task<VenueLocationCreateResponse> CreateVenueLocationAsync(CreateVenueLocationRequest request, int userId)
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

        // Check if venue with same name and address already exists for this owner
        var existingVenue = await _unitOfWork.Context.Set<VenueLocation>()
            .FirstOrDefaultAsync(v => v.VenueOwnerId == venueOwnerProfile.Id 
                && v.Name == request.Name 
                && v.Address == request.Address 
                && v.IsDeleted != true);

        if (existingVenue != null)
        {
            _logger.LogWarning("Venue location with name {VenueName} and address {Address} already exists for user {UserId}", 
                request.Name, request.Address, userId);
            throw new InvalidOperationException($"A venue with name '{request.Name}' at address '{request.Address}' already exists for your account.");
        }

        // Create new venue location entity
        var venueLocation = new VenueLocation
        {
            Name = request.Name,
            Description = request.Description,
            Address = request.Address,
            Email = request.Email,
            PhoneNumber = request.PhoneNumber,
            WebsiteUrl = request.WebsiteUrl,
            PriceMin = request.PriceMin,
            PriceMax = request.PriceMax,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            CoverImage = SerializeImages(request.CoverImage),
            InteriorImage = SerializeImages(request.InteriorImage),
            FullPageMenuImage = SerializeImages(request.FullPageMenuImage),
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

        // Map and return created venue location
        return _mapper.Map<VenueLocationCreateResponse>(venueLocation);
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
        
    
        
        if (request.PriceMin.HasValue)
            venue.PriceMin = request.PriceMin;
        
        if (request.PriceMax.HasValue)
            venue.PriceMax = request.PriceMax;
        
        if (request.Latitude.HasValue)
            venue.Latitude = request.Latitude;
        
        if (request.Longitude.HasValue)
            venue.Longitude = request.Longitude;
        
        if (request.CoverImage != null)
            venue.CoverImage = SerializeImages(request.CoverImage);
        
        if (request.InteriorImage != null)
            venue.InteriorImage = SerializeImages(request.InteriorImage);
        
        if (request.FullPageMenuImage != null)
            venue.FullPageMenuImage = SerializeImages(request.FullPageMenuImage);
        
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
                    IsClosed = request.IsClosed,
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

            // Auto-update IsClosed based on current time
            var currentTimeVN = DateTime.UtcNow.AddHours(7); // Vietnam time (UTC+7)
            var currentTime = currentTimeVN.TimeOfDay;

            openingHour.IsClosed = !(currentTime >= openTime && currentTime < closeTime);

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

    /// <summary>
    /// Automatically update IsClosed status for all venue opening hours based on current time
    /// This method is called by Hangfire as a recurring job every minute
    /// Only updates opening hours for TODAY
    /// </summary>
    public async Task UpdateAllVenuesIsClosedStatusAsync()
    {
        _logger.LogInformation("Starting automatic IsClosed status update for all venues");

        var currentTimeVN = DateTime.UtcNow.AddHours(7); // Vietnam time (UTC+7)
        var currentTime = currentTimeVN.TimeOfDay;
        var todayDbFormat = ConvertDayOfWeekToDbFormat(currentTimeVN.DayOfWeek);

        // ✨ Chỉ lấy opening_hours của HÔM NAY
        var todayOpeningHours = await _unitOfWork.Context.Set<VenueOpeningHour>()
            .Where(oh => oh.Day == todayDbFormat)  // Chỉ ngày hôm nay
            .ToListAsync();

        _logger.LogInformation($"Today's date: {currentTimeVN:yyyy-MM-dd HH:mm:ss}, Day of week (DB format): {todayDbFormat}, Current time (VN): {currentTime}");
        _logger.LogInformation($"Found {todayOpeningHours.Count} opening hour records for today");

        var updatedCount = 0;
        foreach (var openingHour in todayOpeningHours)
        {
            bool isCurrentlyOpen = currentTime >= openingHour.OpenTime && currentTime < openingHour.CloseTime;
            
            // ✨ Nếu user đóng tạm trong giờ mở → Giữ nguyên, không cập nhật
            if (openingHour.IsClosed && isCurrentlyOpen)
            {
                _logger.LogInformation($"Skipped Venue {openingHour.VenueLocationId}: User manually closed during opening hours");
                continue;
            }

            // Tự động update dựa trên thời gian hiện tại
            bool newIsClosed = !isCurrentlyOpen;
            if (openingHour.IsClosed != newIsClosed)
            {
                openingHour.IsClosed = newIsClosed;
                _logger.LogInformation($"Updated Venue {openingHour.VenueLocationId}: OpenTime={openingHour.OpenTime}, CloseTime={openingHour.CloseTime}, IsClosed={openingHour.IsClosed}, IsOpen={isCurrentlyOpen}");
                updatedCount++;
            }
        }

        _unitOfWork.Context.Set<VenueOpeningHour>().UpdateRange(todayOpeningHours);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation($"Completed automatic IsClosed status update. Updated {updatedCount} out of {todayOpeningHours.Count} opening hour records");
    }

    private int ConvertDayOfWeekToDbFormat(DayOfWeek dayOfWeek)
    {
        return dayOfWeek switch
        {
            DayOfWeek.Sunday => 8,
            DayOfWeek.Monday => 2,
            DayOfWeek.Tuesday => 3,
            DayOfWeek.Wednesday => 4,
            DayOfWeek.Thursday => 5,
            DayOfWeek.Friday => 6,
            DayOfWeek.Saturday => 7,
            _ => 8
        };
    }

    /// <summary>
    /// Get all venue locations for a venue owner by user ID
    /// Includes LocationTag details with CoupleMoodType and CouplePersonalityType
    /// </summary>
    public async Task<List<VenueOwnerVenueLocationResponse>> GetVenueLocationsByVenueOwnerAsync(int userId)
    {
        _logger.LogInformation("Getting venue locations for user {UserId}", userId);

        // Find VenueOwnerProfile for the user using repository
        var venueOwnerProfile = await _unitOfWork.VenueOwnerProfiles.GetByUserIdAsync(userId);

        if (venueOwnerProfile == null)
        {
            _logger.LogWarning("User {UserId} does not have a venue owner profile", userId);
            return new List<VenueOwnerVenueLocationResponse>();
        }

        _logger.LogInformation("Found venue owner profile ID {VenueOwnerProfileId} for user {UserId}", venueOwnerProfile.Id, userId);

        // Get venue locations with LocationTag details
        var venueLocations = await _unitOfWork.VenueLocations.GetByVenueOwnerIdWithLocationTagAsync(venueOwnerProfile.Id);

        // Map to response DTOs
        var responses = venueLocations.Select(v => new VenueOwnerVenueLocationResponse
        {
            Id = v.Id,
            Name = v.Name,
            Description = v.Description,
            Address = v.Address,
            Email = v.Email,
            PhoneNumber = v.PhoneNumber,
            WebsiteUrl = v.WebsiteUrl,
            PriceMin = v.PriceMin,
            PriceMax = v.PriceMax,
            Latitude = v.Latitude,
            Longitude = v.Longitude,
            Area = v.Area,
            AverageRating = v.AverageRating,
            AvarageCost = v.AvarageCost,
            ReviewCount = v.ReviewCount,
            Status = v.Status,
            CoverImage = DeserializeImages(v.CoverImage),
            InteriorImage = DeserializeImages(v.InteriorImage),
            Category = v.Category,
            FullPageMenuImage = DeserializeImages(v.FullPageMenuImage),
            IsOwnerVerified = v.IsOwnerVerified,
            CreatedAt = v.CreatedAt,
            UpdatedAt = v.UpdatedAt,
            LocationTag = v.LocationTag != null ? new VenueOwnerLocationTagInfo
            {
                Id = v.LocationTag.Id,
                TagName = GenerateTagName(v.LocationTag.CoupleMoodType?.Name, v.LocationTag.CouplePersonalityType?.Name),
                DetailTag = v.LocationTag.DetailTag,
                CoupleMoodType = v.LocationTag.CoupleMoodType != null ? new VenueOwnerCoupleMoodTypeInfo
                {
                    Id = v.LocationTag.CoupleMoodType.Id,
                    Name = v.LocationTag.CoupleMoodType.Name,
                    Description = v.LocationTag.CoupleMoodType.Description,
                    IsActive = v.LocationTag.CoupleMoodType.IsActive
                } : null,
                CouplePersonalityType = v.LocationTag.CouplePersonalityType != null ? new VenueOwnerCouplePersonalityTypeInfo
                {
                    Id = v.LocationTag.CouplePersonalityType.Id,
                    Name = v.LocationTag.CouplePersonalityType.Name,
                    Description = v.LocationTag.CouplePersonalityType.Description,
                    IsActive = v.LocationTag.CouplePersonalityType.IsActive
                } : null
            } : null
        }).ToList();

        _logger.LogInformation("Retrieved {Count} venue locations for venue owner profile ID {VenueOwnerProfileId}", responses.Count, venueOwnerProfile.Id);

        return responses;
    }
}
