using AutoMapper;
using capstone_backend.Api.Models;
using capstone_backend.Business.DTOs.Common;
using capstone_backend.Business.DTOs.User;
using capstone_backend.Business.DTOs.VenueLocation;
using capstone_backend.Business.Interfaces;
using capstone_backend.Data.Entities;
using capstone_backend.Data.Enums;
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
    private readonly ICurrentUser _currentUser;

    public VenueLocationService(IUnitOfWork unitOfWork, IMapper mapper, ILogger<VenueLocationService> logger, ICurrentUser currentUser)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
        _currentUser = currentUser;
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

        // Check checkin status
        var member = await _unitOfWork.MembersProfile.GetByUserIdAsync(_currentUser.UserId.Value);
        var checkin = await _unitOfWork.CheckInHistories.GetLatestByMemberIdAndVenueIdAsync(member.Id, venueId);

        response.UserState = new UserStateDto
        {
            HasReviewedBefore = await _unitOfWork.Reviews.HasMemberReviewedVenueAsync(member.Id, venueId),
            ActiceCheckInId = checkin != null ? checkin.Id : null,
            CanReview = checkin != null && checkin.IsValid == true
        };

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
    public async Task<VenueReviewsWithSummaryResponse> GetReviewsByVenueIdAsync(int venueId, int page = 1, int pageSize = 10)
    {
        // Lấy danh sách reviews (có phân trang)
        var (reviews, totalCount) = await _unitOfWork.Reviews.GetReviewsByVenueIdAsync(venueId, page, pageSize);

        // Lấy tất cả ratings để tính summary
        var allRatings = await _unitOfWork.Reviews.GetAllRatingsByVenueIdAsync(venueId);

        // Lấy mood match statistics
        var (totalReviewCount, matchedReviewCount) = await _unitOfWork.Reviews.GetMoodMatchStatisticsAsync(venueId);

        // Lấy tất cả media liên quan đến reviews
        var reviewIds = reviews.Select(r => r.Id).ToList();
        var allMedias = await _unitOfWork.Media.GetByListTargetIdsAsync(reviewIds, ReferenceType.REVIEW.ToString());
        var mediaLookup = allMedias.ToLookup(m => m.TargetId);

        // Map reviews sang VenueReviewResponse
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

            // Lấy ImageUrls từ Media
            if (mediaLookup.Contains(r.Id))
            {
                response.ImageUrls = mediaLookup[r.Id].Select(m => m.Url).ToList();
            }
            else
            {
                response.ImageUrls = new List<string>();
            }

            // Set MatchedTag bằng tiếng Việt
            response.MatchedTag = r.IsMatched == true ? "Phù hợp" : "Không phù hợp";

            return response;
        }).ToList();

        // Tính summary statistics
        var summary = CalculateReviewSummary(allRatings, totalReviewCount, matchedReviewCount);

        _logger.LogInformation("Retrieved {Count} reviews for venue {VenueId} (Page {Page}/{TotalPages}) - Average Rating: {AvgRating}, Mood Match: {MoodMatch}%", 
            reviewResponses.Count, venueId, page, (int)Math.Ceiling(totalCount / (double)pageSize), summary.AverageRating, summary.MoodMatchPercentage);

        return new VenueReviewsWithSummaryResponse
        {
            Summary = summary,
            Reviews = new PagedResult<VenueReviewResponse>
            {
                Items = reviewResponses,
                PageNumber = page,
                PageSize = pageSize,
                TotalCount = totalCount
            }
        };
    }

    /// <summary>
    /// Tính toán review summary từ danh sách ratings và mood match statistics
    /// </summary>
    private ReviewSummary CalculateReviewSummary(List<int> ratings, int totalReviewCount, int matchedReviewCount)
    {
        var summary = new ReviewSummary
        {
            TotalReviews = ratings.Count,
            AverageRating = ratings.Any() ? Math.Round((decimal)ratings.Average(), 1) : 0m,
            Ratings = new List<RatingDistribution>(),
            MatchedReviewsCount = matchedReviewCount,
            MoodMatchPercentage = totalReviewCount > 0 
                ? Math.Round((decimal)matchedReviewCount / totalReviewCount * 100, 2) 
                : 0m
        };

        // Tính phân bố ratings từ 5 sao xuống 1 sao
        for (int star = 5; star >= 1; star--)
        {
            var count = ratings.Count(r => r == star);
            var percent = summary.TotalReviews > 0 
                ? Math.Round((decimal)count / summary.TotalReviews * 100, 2) 
                : 0m;

            summary.Ratings.Add(new RatingDistribution
            {
                Star = star,
                Count = count,
                Percent = percent
            });
        }

        return summary;
    }

    /// <summary>
    /// Get reviews for a venue location with optional date/month/year filter, sorted by time with review likes included (có phân trang)
    /// If no date filter provided, returns all reviews
    /// </summary>
    public async Task<VenueReviewsWithSummaryResponse> GetReviewsWithLikesByVenueIdAsync(
        int venueId, 
        int page = 1, 
        int pageSize = 10, 
        DateTime? date = null,
        int? month = null,
        int? year = null,
        bool sortDescending = true)
    {
        // Kiểm tra venue có tồn tại không
        var venue = await _unitOfWork.VenueLocations.GetByIdAsync(venueId);
        if (venue == null || venue.IsDeleted == true)
        {
            _logger.LogWarning("Venue {VenueId} not found or deleted", venueId);
            throw new InvalidOperationException($"Venue location with ID {venueId} not found");
        }

        // Lấy danh sách reviews kèm review likes (có phân trang)
        // Nếu có date filter thì dùng GetReviewsByDateFilterAsync, không thì dùng GetReviewsWithLikesByVenueIdAsync
        var (reviews, totalCount) = (date.HasValue || month.HasValue || year.HasValue)
            ? await _unitOfWork.Reviews.GetReviewsByDateFilterAsync(venueId, page, pageSize, date, month, year, sortDescending)
            : await _unitOfWork.Reviews.GetReviewsWithLikesByVenueIdAsync(venueId, page, pageSize, sortDescending);

        // Lấy tất cả ratings để tính summary
        var allRatings = await _unitOfWork.Reviews.GetAllRatingsByVenueIdAsync(venueId);

        // Lấy mood match statistics
        var (totalReviewCount, matchedReviewCount) = await _unitOfWork.Reviews.GetMoodMatchStatisticsAsync(venueId);

        // Map reviews sang VenueReviewResponse
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

            // Parse ImageUrls từ JSON string sang List<string>
            if (!string.IsNullOrEmpty(r.ImageUrls))
            {
                try
                {
                    response.ImageUrls = System.Text.Json.JsonSerializer.Deserialize<List<string>>(r.ImageUrls);
                }
                catch
                {
                    response.ImageUrls = new List<string>();
                }
            }

            // Set MatchedTag bằng tiếng Việt
            response.MatchedTag = r.IsMatched == true ? "Phù hợp" : "Không phù hợp";

            // Map ReviewLikes
            if (r.ReviewLikes != null && r.ReviewLikes.Any())
            {
                response.ReviewLikes = r.ReviewLikes
                    .Where(rl => rl.Member != null) // Filter out likes without member info
                    .Select(rl => new ReviewLikeInfo
                    {
                        Id = rl.Id,
                        MemberId = rl.MemberId,
                        CreatedAt = rl.CreatedAt,
                        Member = rl.Member != null ? new ReviewMemberInfo
                        {
                            Id = rl.Member.Id,
                            UserId = rl.Member.UserId,
                            FullName = rl.Member.FullName,
                            Gender = rl.Member.Gender,
                            Bio = rl.Member.Bio,
                            DisplayName = rl.Member.User?.DisplayName,
                            AvatarUrl = rl.Member.User?.AvatarUrl,
                            Email = rl.Member.User?.Email
                        } : null
                    })
                    .ToList();
            }
            else
            {
                response.ReviewLikes = new List<ReviewLikeInfo>();
            }

            return response;
        }).ToList();

        // Tính summary statistics
        var summary = CalculateReviewSummary(allRatings, totalReviewCount, matchedReviewCount);

        var filterDescription = date.HasValue
            ? $"date: {date.Value:yyyy-MM-dd}"
            : month.HasValue && year.HasValue
                ? $"month: {year}/{month:D2}"
                : year.HasValue
                    ? $"year: {year}"
                    : "all reviews";

        var sortOrder = sortDescending ? "newest first" : "oldest first";
        _logger.LogInformation(
            "Retrieved {Count} reviews ({Filter}) with likes for venue {VenueId} (Page {Page}/{TotalPages}, {SortOrder}) - Average Rating: {AvgRating}, Mood Match: {MoodMatch}%", 
            reviewResponses.Count, filterDescription, venueId, page, (int)Math.Ceiling(totalCount / (double)pageSize), sortOrder, summary.AverageRating, summary.MoodMatchPercentage);

        return new VenueReviewsWithSummaryResponse
        {
            Summary = summary,
            Reviews = new PagedResult<VenueReviewResponse>
            {
                Items = reviewResponses,
                PageNumber = page,
                PageSize = pageSize,
                TotalCount = totalCount
            }
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
            Status = "DRAFTED",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsDeleted = false,
            AverageRating = null,
            ReviewCount = 0
        };

        // Process multiple tag combinations (many-to-many)
        if (request.VenueTags != null && request.VenueTags.Any())
        {
            var venueLocationTags = new List<VenueLocationTag>();

            foreach (var tagRequest in request.VenueTags)
            {
                // Find LocationTag based on CoupleMoodTypeId + CouplePersonalityTypeId
                var locationTag = await _unitOfWork.LocationTags.GetByMoodAndPersonalityTypeIdsAsync(
                    tagRequest.CoupleMoodTypeId, 
                    tagRequest.CouplePersonalityTypeId);

                if (locationTag != null)
                {
                    venueLocationTags.Add(new VenueLocationTag
                    {
                        LocationTagId = locationTag.Id,
                        CreatedAt = DateTime.UtcNow,
                        IsDeleted = false
                    });
                    _logger.LogInformation("Found location tag ID {LocationTagId} for mood {MoodId} and personality {PersonalityId}",
                        locationTag.Id, tagRequest.CoupleMoodTypeId, tagRequest.CouplePersonalityTypeId);
                }
                else
                {
                    _logger.LogWarning("No location tag found for mood {MoodId} and personality {PersonalityId}",
                        tagRequest.CoupleMoodTypeId, tagRequest.CouplePersonalityTypeId);
                }
            }

            if (venueLocationTags.Any())
            {
                venueLocation.VenueLocationTags = venueLocationTags;
                _logger.LogInformation("Added {Count} location tags to venue", venueLocationTags.Count);
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

        // Update venue tags if provided (soft delete old, restore or add new)
        if (request.VenueTags != null)
        {
            // Soft delete all existing active tags
            var existingTags = await _unitOfWork.Context.Set<VenueLocationTag>()
                .Where(vlt => vlt.VenueLocationId == id && vlt.IsDeleted != true)
                .ToListAsync();
            
            foreach (var tag in existingTags)
            {
                tag.IsDeleted = true;
            }
            _logger.LogInformation("Soft deleted {Count} existing tags for venue {VenueId}", existingTags.Count, id);

            // Add or restore tags
            if (request.VenueTags.Any())
            {
                foreach (var tagRequest in request.VenueTags)
                {
                    var locationTag = await _unitOfWork.LocationTags.GetByMoodAndPersonalityTypeIdsAsync(
                        tagRequest.CoupleMoodTypeId,
                        tagRequest.CouplePersonalityTypeId);

                    if (locationTag != null)
                    {
                        // Check if tag already exists (maybe soft deleted)
                        var existingVenueTag = await _unitOfWork.Context.Set<VenueLocationTag>()
                            .FirstOrDefaultAsync(vlt => vlt.VenueLocationId == id && vlt.LocationTagId == locationTag.Id);

                        if (existingVenueTag != null)
                        {
                            // Restore soft-deleted tag
                            existingVenueTag.IsDeleted = false;
                            _logger.LogInformation("Restored tag {TagId} for venue {VenueId}", locationTag.Id, id);
                        }
                        else
                        {
                            // Create new tag
                            await _unitOfWork.Context.Set<VenueLocationTag>().AddAsync(new VenueLocationTag
                            {
                                VenueLocationId = id,
                                LocationTagId = locationTag.Id,
                                CreatedAt = DateTime.UtcNow,
                                IsDeleted = false
                            });
                            _logger.LogInformation("Added new tag {TagId} for venue {VenueId}", locationTag.Id, id);
                        }
                    }
                    else
                    {
                        _logger.LogWarning("No location tag found for mood {MoodId} and personality {PersonalityId}",
                            tagRequest.CoupleMoodTypeId, tagRequest.CouplePersonalityTypeId);
                    }
                }
            }
        }

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
                } : null,
                CouplePersonalityType = lt.CouplePersonalityType != null ? new CouplePersonalityTypeInfo
                {
                    Id = lt.CouplePersonalityType.Id,
                    Name = lt.CouplePersonalityType.Name,
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
            AverageRating = v.AverageRating.HasValue ? Math.Round(v.AverageRating.Value, 1) : null,
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
            LocationTags = CreateLocationTagsInfo(v)
        }).ToList();

        _logger.LogInformation("Retrieved {Count} venue locations for venue owner profile ID {VenueOwnerProfileId}", responses.Count, venueOwnerProfile.Id);

        return responses;
    }
    /// <summary>
    /// Submit venue location to admin for approval
    /// Validates required fields before changing status to PENDING
    /// </summary>
    public async Task<VenueSubmissionResult> SubmitVenueToAdminAsync(int venueId, int userId)
    {
        _logger.LogInformation("Submitting venue {VenueId} to admin for user {UserId}", venueId, userId);
        
        // 1. Get venue with details to check opening hours
        var venue = await _unitOfWork.VenueLocations.GetByIdWithDetailsAsync(venueId);
        
        if (venue == null || venue.IsDeleted == true)
        {
             return new VenueSubmissionResult 
             { 
                 IsSuccess = false, 
                 Message = "Venue not found" 
             };
        }

        // 2. Validate Owner
        var ownerProfile = await _unitOfWork.VenueOwnerProfiles.GetByUserIdAsync(userId);
        if (ownerProfile == null || venue.VenueOwnerId != ownerProfile.Id)
        {
             _logger.LogWarning("User {UserId} attempted to submit venue {VenueId} but is not the owner", userId, venueId);
             return new VenueSubmissionResult 
             { 
                 IsSuccess = false, 
                 Message = "Unauthorized access" 
             };
        }

        // 3. Check Status (Allow DRAFT or DRAFTED just in case)
        if (venue.Status != "DRAFTED" && venue.Status != "DRAFT")
        {
             return new VenueSubmissionResult 
             { 
                 IsSuccess = false, 
                 Message = $"Venue status is {venue.Status}, cannot submit. Only drafted venues can be submitted." 
             };
        }
        
        // 4. Validate Fields
        var missingFields = new List<string>();

        if (string.IsNullOrWhiteSpace(venue.Name)) missingFields.Add("Name");
        if (string.IsNullOrWhiteSpace(venue.Description)) missingFields.Add("Description");
        if (string.IsNullOrWhiteSpace(venue.Address)) missingFields.Add("Address");
        
        // Check Images
        var coverImages = DeserializeImages(venue.CoverImage);
        if (coverImages == null || !coverImages.Any()) missingFields.Add("CoverImage");
        
        // Contact (Both Phone AND Email are required)
        if (string.IsNullOrWhiteSpace(venue.PhoneNumber)) missingFields.Add("Phone Number");
        if (string.IsNullOrWhiteSpace(venue.Email)) missingFields.Add("Email");
        
        // Location Tag - kiểm tra có ít nhất 1 tag
        if (!venue.VenueLocationTags.Any()) missingFields.Add("LocationTag");
        
        // Coordinates
        if (venue.Latitude == null || venue.Longitude == null) 
        {
            missingFields.Add("Location Coordinates (Latitude or Longitude)");
        }
        
        // Price
        if (venue.PriceMin == null || venue.PriceMax == null) 
        {
            missingFields.Add("Price Range (Min/Max)");
        }
        
        // Opening Hours
      

        if (missingFields.Any())
        {
            return new VenueSubmissionResult 
            { 
                IsSuccess = false, 
                Message = "Please fill in all required fields before submitting.", 
                MissingFields = missingFields 
            };
        }

        // 5. Update Status
        venue.Status = "PENDING";
        venue.UpdatedAt = DateTime.UtcNow;
        
        _unitOfWork.VenueLocations.Update(venue);
        await _unitOfWork.SaveChangesAsync();
        
        _logger.LogInformation("Venue {VenueId} submitted successfully, status changed to PENDING", venueId);
        
        return new VenueSubmissionResult 
        { 
            IsSuccess = true, 
            Message = "Venue submitted successfully. Please wait for admin approval." 
        };
    }
    /// <summary>
    /// Get pending venue locations for admin approval
    /// </summary>
    public async Task<PagedResult<VenueOwnerVenueLocationResponse>> GetPendingVenuesAsync(int page, int pageSize)
    {
        _logger.LogInformation("Retrieving pending venue locations (Page {Page}, Size {PageSize})", page, pageSize);

        var (venues, totalCount) = await _unitOfWork.VenueLocations.GetPendingVenuesAsync(page, pageSize);

        var responses = venues.Select(v => new VenueOwnerVenueLocationResponse
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
            AverageRating = v.AverageRating.HasValue ? Math.Round(v.AverageRating.Value, 1) : null,
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
            LocationTags = CreateLocationTagsInfo(v)
        }).ToList();

        _logger.LogInformation("Retrieved {Count} pending venue locations", responses.Count);

        return new PagedResult<VenueOwnerVenueLocationResponse>
        {
            Items = responses,
            PageNumber = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };

        
    }

      /// <summary>
    /// Approve or reject a venue location
    /// Only allows status PENDING -> ACTIVE or PENDING -> DRAFTED
    /// </summary>
    public async Task<VenueSubmissionResult> ApproveVenueAsync(VenueApprovalRequest request)
    {
        _logger.LogInformation("Processing venue approval request for Venue {VenueId}, Status: {Status}", request.VenueId, request.Status);

        // Validate Status
        var status = request.Status?.ToUpper();
        if (status != "ACTIVE" && status != "DRAFTED")
        {
            return new VenueSubmissionResult { IsSuccess = false, Message = "Invalid status. Only 'ACTIVE' or 'DRAFTED' are allowed." };
        }

        var venue = await _unitOfWork.VenueLocations.GetByIdAsync(request.VenueId);
        
        if (venue == null || venue.IsDeleted == true)
        {
            return new VenueSubmissionResult { IsSuccess = false, Message = "Venue not found" };
        }

        // Only allow PENDING venues to be approved/rejected
        // Wait, user might want to ban an ACTIVE venue back to DRAFTED? 
        // User request: "approve location" implies pending -> active. "reject" implies pending -> drafted.
        // Let's strict check PENDING for now, unless user specified otherwise.
        if (venue.Status != "PENDING")
        {
             return new VenueSubmissionResult { IsSuccess = false, Message = $"Cannot approve/reject venue with status '{venue.Status}'. Only 'PENDING' venues can be processed." };
        }

        venue.Status = status;
        venue.UpdatedAt = DateTime.UtcNow;
        
        // Fix DateTime fields for PostgreSQL
        if (venue.CreatedAt.HasValue && venue.CreatedAt.Value.Kind == DateTimeKind.Unspecified)
            venue.CreatedAt = DateTime.SpecifyKind(venue.CreatedAt.Value, DateTimeKind.Utc);
        // If rejected, maybe append reason to description or send notification (out of scope for now)
        if (status == "DRAFTED" && !string.IsNullOrEmpty(request.Reason))
        {
            _logger.LogInformation("Venue {VenueId} rejected. Reason: {Reason}", request.VenueId, request.Reason);
        }

        _unitOfWork.VenueLocations.Update(venue);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Venue {VenueId} status updated to {Status}", request.VenueId, status);

        return new VenueSubmissionResult { IsSuccess = true, Message = $"Venue {status} successfully" };
    }

    /// <summary>
    /// Helper method to create list of VenueOwnerLocationTagInfo from VenueLocation (many-to-many)
    /// </summary>
    private List<VenueOwnerLocationTagInfo>? CreateLocationTagsInfo(VenueLocation venue)
    {
        if (venue.VenueLocationTags == null || !venue.VenueLocationTags.Any())
            return null;

        return venue.VenueLocationTags
            .Where(vlt => vlt.LocationTag != null && vlt.IsDeleted != true)
            .Select(vlt => new VenueOwnerLocationTagInfo
            {
                Id = vlt.LocationTag!.Id,
                TagName = GenerateTagName(vlt.LocationTag.CoupleMoodType?.Name, vlt.LocationTag.CouplePersonalityType?.Name),
                DetailTag = vlt.LocationTag.DetailTag,
                CoupleMoodType = vlt.LocationTag.CoupleMoodType != null ? new VenueOwnerCoupleMoodTypeInfo
                {
                    Id = vlt.LocationTag.CoupleMoodType.Id,
                    Name = vlt.LocationTag.CoupleMoodType.Name,
                    Description = vlt.LocationTag.CoupleMoodType.Description,
                    IsActive = vlt.LocationTag.CoupleMoodType.IsActive
                } : null,
                CouplePersonalityType = vlt.LocationTag.CouplePersonalityType != null ? new VenueOwnerCouplePersonalityTypeInfo
                {
                    Id = vlt.LocationTag.CouplePersonalityType.Id,
                    Name = vlt.LocationTag.CouplePersonalityType.Name,
                    Description = vlt.LocationTag.CouplePersonalityType.Description,
                    IsActive = vlt.LocationTag.CouplePersonalityType.IsActive
                } : null
            })
            .ToList();
    }

    /// <summary>
    /// Delete (soft delete) a location tag from venue
    /// Venue must have at least 2 active tags to allow deletion
    /// </summary>
    public async Task<bool> DeleteVenueLocationTagAsync(int venueId, int locationTagId)
    {
        _logger.LogInformation("Attempting to delete tag {TagId} from venue {VenueId}", locationTagId, venueId);

        // Check if venue exists
        var venue = await _unitOfWork.VenueLocations.GetByIdAsync(venueId);
        if (venue == null || venue.IsDeleted == true)
        {
            _logger.LogWarning("Venue {VenueId} not found or deleted", venueId);
            return false;
        }

        // Get all active tags for this venue
        var activeTags = await _unitOfWork.Context.Set<VenueLocationTag>()
            .Where(vlt => vlt.VenueLocationId == venueId && vlt.IsDeleted != true)
            .ToListAsync();

        // Check if venue has at least 2 tags (cannot delete last tag)
        if (activeTags.Count <= 1)
        {
            _logger.LogWarning("Cannot delete last tag from venue {VenueId}", venueId);
            return false;
        }

        // Find the tag to delete
        var tagToDelete = activeTags.FirstOrDefault(vlt => vlt.LocationTagId == locationTagId);
        if (tagToDelete == null)
        {
            _logger.LogWarning("Tag {TagId} not found for venue {VenueId}", locationTagId, venueId);
            return false;
        }

        // Soft delete
        tagToDelete.IsDeleted = true;

        await _unitOfWork.SaveChangesAsync();
        _logger.LogInformation("Soft deleted tag {TagId} from venue {VenueId}", locationTagId, venueId);

        return true;
    }
}
