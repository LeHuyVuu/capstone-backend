using AutoMapper;
using capstone_backend.Api.Models;
using capstone_backend.Api.VenueRecommendation.Service;
using capstone_backend.Business.DTOs.Accessory;
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
    private readonly SepayService _sepayService;
    private readonly IMeilisearchService _meilisearchService;
    private readonly RefundService _refundService;
    private readonly IEmailService _emailService;
    private readonly WalletPaymentService _walletPaymentService;
    private readonly IAccessoryService _accessoryService;

    public VenueLocationService(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<VenueLocationService> logger,
        ICurrentUser currentUser,
        SepayService sepayService,
        IMeilisearchService meilisearchService,
        RefundService refundService,
        IEmailService emailService,
        WalletPaymentService walletPaymentService,
        IAccessoryService accessoryService)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
        _currentUser = currentUser;
        _sepayService = sepayService;
        _meilisearchService = meilisearchService;
        _refundService = refundService;
        _emailService = emailService;
        _walletPaymentService = walletPaymentService;
        _accessoryService = accessoryService;
    }

    #region Category & Image Helpers

    /// <summary>
    /// Serialize list of categories to string (format: "CATEGORY1 / CATEGORY2 / CATEGORY3")
    /// </summary>
    private static string? SerializeCategories(List<string>? categories)
    {
        if (categories == null || categories.Count == 0)
            return null;
        
        // Filter out empty strings and join with " / "
        var validCategories = categories
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .Select(c => c.Trim())
            .ToList();
        
        return validCategories.Any() ? string.Join(" / ", validCategories) : null;
    }
    
    /// <summary>
    /// Deserialize category string to list (split by " / ")
    /// </summary>
    private static List<string>? DeserializeCategory(string? categoryString)
    {
        if (string.IsNullOrWhiteSpace(categoryString))
            return null;
        
        return categoryString
            .Split(new[] { " / " }, StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim())
            .Where(s => !string.IsNullOrEmpty(s))
            .ToList();
    }
    
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
    /// Handles multiple formats: JSON array, single string, or malformed strings
    /// </summary>
    public static List<string>? DeserializeImages(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;

        // Clean up the string - remove surrounding quotes (single or double)
        var cleaned = json.Trim();
        
        // Remove leading/trailing single quotes if present
        if (cleaned.StartsWith("'") && cleaned.EndsWith("'"))
        {
            cleaned = cleaned.Substring(1, cleaned.Length - 2).Trim();
        }
        
        // Remove leading/trailing double quotes if it's a quoted JSON string
        if (cleaned.StartsWith("\"") && cleaned.EndsWith("\"") && cleaned.Length > 2)
        {
            // Only remove if it looks like a quoted JSON array
            if (cleaned.Contains("["))
            {
                cleaned = cleaned.Substring(1, cleaned.Length - 2).Trim();
            }
        }

        try
        {
            // Try to deserialize as JSON array
            if (cleaned.TrimStart().StartsWith("["))
            {
                var result = JsonSerializer.Deserialize<List<string>>(cleaned);
                // Filter out empty strings and return
                return result?.Where(s => !string.IsNullOrWhiteSpace(s)).ToList();
            }
            
            // If it's a single URL, return as array with one element
            return new List<string> { cleaned };
        }
        catch (JsonException ex)
        {
            // Log the error for debugging
            Console.WriteLine($"[WARNING] Failed to deserialize image JSON: {ex.Message}. Raw value: {json}");
            
            // Try to extract URLs from malformed string
            if (cleaned.Contains("http"))
            {
                // Extract all URLs using basic pattern matching
                var urls = System.Text.RegularExpressions.Regex.Matches(cleaned, @"https?://[^\s,\""'\]]+")
                    .Cast<System.Text.RegularExpressions.Match>()
                    .Select(m => m.Value)
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .ToList();
                
                if (urls.Any())
                    return urls;
            }
            
            // Last resort: return the original string as a single element
            return new List<string> { cleaned };
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
        response.Category = DeserializeCategory(venue.Category);
        response.CoverImage = DeserializeImages(venue.CoverImage);
        response.InteriorImage = DeserializeImages(venue.InteriorImage);
        response.FullPageMenuImage = DeserializeImages(venue.FullPageMenuImage);

        // Map categories from VenueLocationCategories
        response.Categories = venue.VenueLocationCategories?
            .Where(vlc => !vlc.IsDeleted && vlc.Category != null && !vlc.Category.IsDeleted)
            .Select(vlc => new CategoryInfo
            {
                Id = vlc.Category.Id,
                Name = vlc.Category.Name
            })
            .ToList();

        // Add today's opening hour info
        var todayOpeningHour = venue.VenueOpeningHours?.FirstOrDefault();
        if (todayOpeningHour != null)
        {
            var currentTimeVN = DateTime.UtcNow.AddHours(7);
            response.TodayDayName = GetDayName(currentTimeVN.DayOfWeek);
            response.TodayOpeningHour = _mapper.Map<TodayOpeningHourResponse>(todayOpeningHour);
            
            // Tính status: Ưu tiên IsClosed, sau đó mới tính theo giờ
            if (todayOpeningHour.IsClosed)
            {
                response.TodayOpeningHour.Status = "Đã đóng cửa";
            }
            else
            {
                var currentTime = currentTimeVN.TimeOfDay;
                var openTime = todayOpeningHour.OpenTime;
                var closeTime = todayOpeningHour.CloseTime;
                
                // Check nếu đang trong giờ mở cửa
                bool isOpen = closeTime < openTime 
                    ? (currentTime >= openTime || currentTime < closeTime)  // Qua đêm (VD: 23:00-02:00)
                    : (currentTime >= openTime && currentTime < closeTime); // Bình thường (VD: 08:00-22:00)
                
                response.TodayOpeningHour.Status = isOpen ? "Đang mở cửa" : "Đã đóng cửa";
            }
        }

        // Check checkin status
        if (_currentUser.UserId != null && _currentUser.Role == "MEMBER")
        {
            var member = await _unitOfWork.MembersProfile.GetByUserIdAsync(_currentUser.UserId.Value);
            var checkin = await _unitOfWork.CheckInHistories.GetLatestByMemberIdAndVenueIdAsync(member.Id, venueId);
            var couple = await _unitOfWork.CoupleProfiles.GetActiveCoupleByMemberIdAsync(member.Id);
            int? coupleProfileId = couple?.id;

            response.UserState = new UserStateDto
            {
                HasReviewedBefore = await _unitOfWork.Reviews.HasMemberReviewedVenueAsync(member.Id, venueId, coupleProfileId),
                ActiceCheckInId = checkin != null ? checkin.Id : null,
                CanReview = checkin != null && checkin.IsValid == true
            };
        }
        

        _logger.LogInformation("Retrieved venue location detail for ID {VenueId}", venueId);

        return response;
    }

    /// <summary>
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
        int? currentMemberId = null;
        int? currentCoupleId = null;
        int? partnerMemberId = null;

        if (_currentUser?.UserId != null)
        {
            var member = await _unitOfWork.MembersProfile.GetByUserIdAsync(_currentUser.UserId.Value);
            if (member != null)
            {
                currentMemberId = member.Id;
                var currentCouple = await _unitOfWork.CoupleProfiles.GetActiveCoupleByMemberIdAsync(member.Id);
                if (currentCouple != null)
                {
                    currentCoupleId = currentCouple.id;
                    partnerMemberId = currentCouple.MemberId1 == member.Id ? currentCouple.MemberId2 : currentCouple.MemberId1;
                }
            }
        }

        // Lấy danh sách reviews (có phân trang)
        var (reviews, totalCount) = await _unitOfWork.Reviews.GetReviewsByVenueIdAsync(venueId, page, pageSize, currentMemberId, currentCoupleId, partnerMemberId);

        // Lấy tất cả ratings để tính summary
        var allRatings = await _unitOfWork.Reviews.GetAllRatingsByVenueIdAsync(venueId);

        // Lấy mood match statistics
        var (totalReviewCount, matchedReviewCount) = await _unitOfWork.Reviews.GetMoodMatchStatisticsAsync(venueId);

        // Lấy tất cả media liên quan đến reviews
        var reviewIds = reviews.Select(r => r.Id).ToList();
        var allMedias = await _unitOfWork.Media.GetByListTargetIdsAsync(reviewIds, ReferenceType.REVIEW.ToString());
        var mediaLookup = allMedias.ToLookup(m => m.TargetId);

        var venue = await _unitOfWork.VenueLocations.GetByIdWithOwnerAsync(venueId);

        var memberIds = reviews
            .Where(r => r.IsAnonymous != true && r.Member != null)
            .Select(r => r.MemberId)
            .Distinct()
            .ToList();

        var accessoryLookup = new Dictionary<int, List<EquippedAccessoryBriefResponse>>();
        foreach (var memberId in memberIds)
        {
            accessoryLookup[memberId] = await _accessoryService.GetEquippedAccessoryForMemberAsync(memberId);
        }

        // Map reviews sang VenueReviewResponse
        var reviewResponses = reviews.Select(r => 
        {
            var response = _mapper.Map<VenueReviewResponse>(r);
            response.IsOwner = currentMemberId.HasValue && r.MemberId == currentMemberId.Value;
            response.IsLikedByMe = r.ReviewLikes != null && currentMemberId.HasValue && r.ReviewLikes.Any(rl => rl.MemberId == currentMemberId.Value);

            // Map member information
            if (r.IsAnonymous == true && response.IsOwner == false)
            {
                response.Member = new ReviewMemberInfo
                {
                    Id = 0,
                    UserId = 0,
                    FullName = "Ẩn danh",
                    DisplayName = "Ẩn danh",
                    AvatarUrl = "https://couplemood-store.s3.ap-southeast-2.amazonaws.com/system/anonymous.png",
                    EquippedAccessories = new List<EquippedAccessoryBriefResponse>()
                };
            }
            else if (r.Member != null)
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
                    Email = r.Member.User?.Email,
                    EquippedAccessories = accessoryLookup.TryGetValue(r.MemberId, out var accessories) ? accessories : new List<EquippedAccessoryBriefResponse>()
                };
            }

            response.IsAnonymous = r.IsAnonymous;

            // Lấy ImageUrls từ Media
            if (mediaLookup.Contains(r.Id))
            {
                response.ImageUrls = mediaLookup[r.Id].Select(m => m.Url).ToList();
            }
            else
            {
                response.ImageUrls = new List<string>();
            }

            if (r.ReviewReply != null)
            {
                if (response.ReviewReply != null)
                {
                    response.ReviewReply.VenueId = venue.Id;
                    response.ReviewReply.VenueName = venue.Name;
                    response.ReviewReply.VenueCoverImage = venue.CoverImage != null ? DeserializeImages(venue.CoverImage) : null;
                }
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
        int? currentMemberId = null;

        if (_currentUser?.UserId != null)
        {
            var member = await _unitOfWork.MembersProfile.GetByUserIdAsync(_currentUser.UserId.Value);
            if (member != null)
            {
                currentMemberId = member.Id;
            }
        }

        // Kiểm tra venue có tồn tại không
        var venue = await _unitOfWork.VenueLocations.GetByIdWithOwnerAsync(venueId);
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

        // Lấy tất cả media liên quan đến reviews
        var reviewIds = reviews.Select(r => r.Id).ToList();
        var allMedias = await _unitOfWork.Media.GetByListTargetIdsAsync(reviewIds, ReferenceType.REVIEW.ToString());
        var mediaLookup = allMedias.ToLookup(m => m.TargetId);

        // Get accessories
        var memberIds = reviews
            .Where(r => r.IsAnonymous != true && r.Member != null)
            .Select(r => r.MemberId)
            .Distinct()
            .ToList();

        var accessoryLookup = new Dictionary<int, List<EquippedAccessoryBriefResponse>>();
        foreach (var memberId in memberIds)
        {
            accessoryLookup[memberId] = await _accessoryService.GetEquippedAccessoryForMemberAsync(memberId);
        }

        // Map reviews sang VenueReviewResponse
        var reviewResponses = reviews.Select(r => 
        {
            var response = _mapper.Map<VenueReviewResponse>(r);
            response.IsOwner = currentMemberId.HasValue && r.MemberId == currentMemberId.Value;
            response.IsLikedByMe = r.ReviewLikes != null && currentMemberId.HasValue && r.ReviewLikes.Any(rl => rl.MemberId == currentMemberId.Value);

            // Map member information
            if (r.IsAnonymous == true && response.IsOwner == false)
            {
                response.Member = new ReviewMemberInfo
                {
                    Id = 0,
                    UserId = 0,
                    FullName = "Ẩn danh",
                    DisplayName = "Ẩn danh",
                    AvatarUrl = "https://couplemood-store.s3.ap-southeast-2.amazonaws.com/system/anonymous.png",
                    EquippedAccessories = new List<EquippedAccessoryBriefResponse>()
                };
            }
            else if (r.Member != null)
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
                    Email = r.Member.User?.Email,
                    EquippedAccessories = accessoryLookup.TryGetValue(r.MemberId, out var accessories) ? accessories : new List<EquippedAccessoryBriefResponse>()
                };
            }

            response.IsAnonymous = r.IsAnonymous;

            // Lấy ImageUrls từ Media
            if (mediaLookup.Contains(r.Id))
            {
                response.ImageUrls = mediaLookup[r.Id].Select(m => m.Url).ToList();
            }
            else
            {
                response.ImageUrls = new List<string>();
            }

            if (r.ReviewReply != null)
            {
                if (response.ReviewReply != null)
                {
                    response.ReviewReply.VenueId = venue.Id;
                    response.ReviewReply.VenueName = venue.Name;
                    response.ReviewReply.VenueCoverImage = venue.CoverImage != null ? DeserializeImages(venue.CoverImage) : null;
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
            Category = null, // Will be set after saving categories
            CoverImage = SerializeImages(request.CoverImage),
            InteriorImage = SerializeImages(request.InteriorImage),
            FullPageMenuImage = SerializeImages(request.FullPageMenuImage),
            IsOwnerVerified = request.IsOwnerVerified ?? false,
            BusinessLicenseUrl = request.BusinessLicenseUrl,
            VenueOwnerId = venueOwnerProfile.Id,
            Status = VenueLocationStatus.DRAFTED.ToString(),
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

        // Save categories to VenueLocationCategory table and build category string
        if (request.CategoryIds != null && request.CategoryIds.Any())
        {
            var categoryNames = new List<string>();
            foreach (var categoryId in request.CategoryIds)
            {
                var category = await _unitOfWork.Categories.GetByIdAsync(categoryId);
                if (category != null && !category.IsDeleted)
                {
                    categoryNames.Add(category.Name);
                    await _unitOfWork.Context.Set<VenueLocationCategory>().AddAsync(new VenueLocationCategory
                    {
                        VenueLocationId = venueLocation.Id,
                        CategoryId = category.Id,
                        CreatedAt = DateTime.UtcNow,
                        IsDeleted = false
                    });
                }
            }
            if (categoryNames.Any())
            {
                venueLocation.Category = string.Join(" / ", categoryNames);
            }
            await _unitOfWork.SaveChangesAsync();
        }

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
        
        if (request.BusinessLicenseUrl != null)
            venue.BusinessLicenseUrl = request.BusinessLicenseUrl;
        
        // Update categories only if explicitly provided (not null)
        // If CategoryIds is null, keep the existing categories unchanged
        if (request.CategoryIds != null)
        {
            var categoryNames = new List<string>();
            
            // Hard delete all old categories
            var oldCategories = await _unitOfWork.Context.Set<VenueLocationCategory>()
                .Where(vlc => vlc.VenueLocationId == id).ToListAsync();
            _unitOfWork.Context.Set<VenueLocationCategory>().RemoveRange(oldCategories);
            
            // Insert new categories
            foreach (var categoryId in request.CategoryIds)
            {
                var category = await _unitOfWork.Categories.GetByIdAsync(categoryId);
                if (category != null && !category.IsDeleted)
                {
                    categoryNames.Add(category.Name);
                    await _unitOfWork.Context.Set<VenueLocationCategory>().AddAsync(new VenueLocationCategory
                    {
                        VenueLocationId = id,
                        CategoryId = category.Id,
                        CreatedAt = DateTime.UtcNow,
                        IsDeleted = false
                    });
                }
            }
            
            venue.Category = categoryNames.Any() ? string.Join(" / ", categoryNames) : null;
        }

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
                Description = mt.Description,
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
    /// Automatically updates is_closed based on current time or manual override
    /// 
    /// IsClosed Priority:
    /// 1. Manual override (if request.IsClosed is provided)
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

        var activeSubscriptionsByVenueId = await GetActiveVenueSubscriptionsByVenueIdsAsync(
            venueLocations.Select(v => v.Id).ToList());

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
            RejectionDetails = string.IsNullOrWhiteSpace(v.RejectReason) ? null : System.Text.Json.JsonSerializer.Deserialize<List<RejectionRecord>>(v.RejectReason),
            CreatedAt = v.CreatedAt,
            UpdatedAt = v.UpdatedAt,
            DurationDays = activeSubscriptionsByVenueId.TryGetValue(v.Id, out var activeSubscription)
                ? CalculateActualDurationDays(activeSubscription)
                : null,
            StartDate = activeSubscriptionsByVenueId.TryGetValue(v.Id, out activeSubscription)
                ? activeSubscription.StartDate
                : null,
            EndDate = activeSubscriptionsByVenueId.TryGetValue(v.Id, out activeSubscription)
                ? activeSubscription.EndDate
                : null,
            LocationTags = CreateLocationTagsInfo(v)
        }).ToList();

        _logger.LogInformation("Retrieved {Count} venue locations for venue owner profile ID {VenueOwnerProfileId}", responses.Count, venueOwnerProfile.Id);

        return responses;
    }

    public async Task<VenueOwnerVenueLocationResponse?> GetVenueLocationByIdForOwnerAsync(int venueId, int userId)
    {
        _logger.LogInformation("Getting venue location {VenueId} for user {UserId}", venueId, userId);

        // Find VenueOwnerProfile for the user
        var venueOwnerProfile = await _unitOfWork.VenueOwnerProfiles.GetByUserIdAsync(userId);

        if (venueOwnerProfile == null)
        {
            _logger.LogWarning("User {UserId} does not have a venue owner profile", userId);
            return null;
        }

        // Get venue location with LocationTag details
        var venueLocations = await _unitOfWork.VenueLocations.GetByVenueOwnerIdWithLocationTagAsync(venueOwnerProfile.Id);
        var venue = venueLocations.FirstOrDefault(v => v.Id == venueId);

        if (venue == null)
        {
            _logger.LogWarning("Venue {VenueId} not found or not owned by user {UserId}", venueId, userId);
            return null;
        }

        var activeSubscriptionsByVenueId = await GetActiveVenueSubscriptionsByVenueIdsAsync(new List<int> { venueId });

        // Map to response DTO
        var response = new VenueOwnerVenueLocationResponse
        {
            Id = venue.Id,
            Name = venue.Name,
            Description = venue.Description,
            Address = venue.Address,
            Email = venue.Email,
            PhoneNumber = venue.PhoneNumber,
            WebsiteUrl = venue.WebsiteUrl,
            PriceMin = venue.PriceMin,
            PriceMax = venue.PriceMax,
            Latitude = venue.Latitude,
            Longitude = venue.Longitude,
            Area = venue.Area,
            AverageRating = venue.AverageRating.HasValue ? Math.Round(venue.AverageRating.Value, 1) : null,
            AvarageCost = venue.AvarageCost,
            ReviewCount = venue.ReviewCount,
            Status = venue.Status,
            CoverImage = DeserializeImages(venue.CoverImage),
            InteriorImage = DeserializeImages(venue.InteriorImage),
            Category = venue.Category,
            FullPageMenuImage = DeserializeImages(venue.FullPageMenuImage),
            IsOwnerVerified = venue.IsOwnerVerified,
            RejectionDetails = string.IsNullOrWhiteSpace(venue.RejectReason) ? null : System.Text.Json.JsonSerializer.Deserialize<List<RejectionRecord>>(venue.RejectReason),
            CreatedAt = venue.CreatedAt,
            UpdatedAt = venue.UpdatedAt,
            DurationDays = activeSubscriptionsByVenueId.TryGetValue(venue.Id, out var activeSubscription)
                ? CalculateActualDurationDays(activeSubscription)
                : null,
            StartDate = activeSubscriptionsByVenueId.TryGetValue(venue.Id, out activeSubscription)
                ? activeSubscription.StartDate
                : null,
            EndDate = activeSubscriptionsByVenueId.TryGetValue(venue.Id, out activeSubscription)
                ? activeSubscription.EndDate
                : null,
            LocationTags = CreateLocationTagsInfo(venue)
        };

        _logger.LogInformation("Retrieved venue location {VenueId} for user {UserId}", venueId, userId);

        return response;
    }

    public async Task<PagedResult<VenueOwnerVenueLocationResponse>> GetVenueLocationsByVenueOwnerAndStatusAsync(VenueLocationStatus? status, string? search, int page, int pageSize)
    {
        _logger.LogInformation("Getting all system venue locations with status {Status}, search {Search} (Page {Page}, Size {PageSize})", status, search, page, pageSize);

        var normalizedSearch = search?.Trim();
        var hasSearch = !string.IsNullOrWhiteSpace(normalizedSearch);
        var likePattern = hasSearch ? $"%{EscapeSqlLikePattern(normalizedSearch!)}%" : null;

        var (venueLocations, totalCount) = await _unitOfWork.VenueLocations.GetPagedAsync(
            page,
            pageSize,
            v => v.IsDeleted != true
                && (!status.HasValue || v.Status == status.Value.ToString())
                && (!hasSearch
                    || (v.Name != null && EF.Functions.Like(v.Name, likePattern!))
                    || (v.Description != null && EF.Functions.Like(v.Description, likePattern!))
                    || (v.Address != null && EF.Functions.Like(v.Address, likePattern!))),
            query => query.OrderByDescending(v => v.CreatedAt),
            query => query
                .Include(v => v.VenueLocationTags)
                    .ThenInclude(vlt => vlt.LocationTag)
                        .ThenInclude(lt => lt!.CoupleMoodType)
                .Include(v => v.VenueLocationTags)
                    .ThenInclude(vlt => vlt.LocationTag)
                        .ThenInclude(lt => lt!.CouplePersonalityType)
                .AsSplitQuery());

        var activeSubscriptionsByVenueId = await GetActiveVenueSubscriptionsByVenueIdsAsync(
            venueLocations.Select(v => v.Id).ToList());

        var responses = venueLocations
            .Select(v => new VenueOwnerVenueLocationResponse
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
                RejectionDetails = string.IsNullOrWhiteSpace(v.RejectReason) ? null : System.Text.Json.JsonSerializer.Deserialize<List<RejectionRecord>>(v.RejectReason),
                CreatedAt = v.CreatedAt,
                UpdatedAt = v.UpdatedAt,
                DurationDays = activeSubscriptionsByVenueId.TryGetValue(v.Id, out var activeSubscription)
                    ? CalculateActualDurationDays(activeSubscription)
                    : null,
                StartDate = activeSubscriptionsByVenueId.TryGetValue(v.Id, out activeSubscription)
                    ? activeSubscription.StartDate
                    : null,
                EndDate = activeSubscriptionsByVenueId.TryGetValue(v.Id, out activeSubscription)
                    ? activeSubscription.EndDate
                    : null,
                LocationTags = CreateLocationTagsInfo(v)
            }).ToList();

        _logger.LogInformation("Retrieved {Count} system venue locations with status {Status}, search {Search} (Total {TotalCount})", responses.Count, status, search, totalCount);

        return new PagedResult<VenueOwnerVenueLocationResponse>(responses, page, pageSize, totalCount);
    }

    private async Task<Dictionary<int, VenueSubscriptionPackage>> GetActiveVenueSubscriptionsByVenueIdsAsync(List<int> venueIds)
    {
        if (venueIds.Count == 0)
        {
            return new Dictionary<int, VenueSubscriptionPackage>();
        }

        var now = DateTime.UtcNow;

        var activeSubscriptions = await _unitOfWork.Context.Set<VenueSubscriptionPackage>()
            .AsNoTracking()
            .Include(vsp => vsp.Package)
            .Where(vsp => venueIds.Contains(vsp.VenueId)
                && vsp.Status == VenueSubscriptionPackageStatus.ACTIVE.ToString()
                && (!vsp.StartDate.HasValue || vsp.StartDate.Value <= now)
                && (!vsp.EndDate.HasValue || vsp.EndDate.Value >= now))
            .OrderByDescending(vsp => vsp.CreatedAt)
            .ThenByDescending(vsp => vsp.Id)
            .ToListAsync();

        return activeSubscriptions
            .GroupBy(vsp => vsp.VenueId)
            .ToDictionary(group => group.Key, group => group.First());
    }

    private static int? CalculateActualDurationDays(VenueSubscriptionPackage subscription)
    {
        if (subscription.StartDate.HasValue && subscription.EndDate.HasValue)
        {
            var durationDays = (int)Math.Ceiling((subscription.EndDate.Value - subscription.StartDate.Value).TotalDays);
            return Math.Max(durationDays, 0);
        }

        if (subscription.Package?.DurationDays.HasValue == true)
        {
            var quantity = subscription.Quantity ?? 1;
            return subscription.Package.DurationDays.Value * quantity;
        }

        return null;
    }

    private static string EscapeSqlLikePattern(string input)
    {
        return input
            .Replace("[", "[[]")
            .Replace("%", "[%]")
            .Replace("_", "[_]");
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
        if (venue.Status != VenueLocationStatus.DRAFTED.ToString())
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
        venue.Status = VenueLocationStatus.PENDING.ToString();
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
    /// Submit venue with payment - validates venue, creates subscription & transaction, generates QR code
    /// </summary>
    public async Task<SubmitVenueWithPaymentResponse> SubmitVenueWithPaymentAsync(
        int venueId, 
        int userId, 
        SubmitVenueWithPaymentRequest request)
    {
        _logger.LogInformation("Submitting venue {VenueId} with payment - UserId: {UserId}, PackageId: {PackageId}, Qty: {Qty}",
            venueId, userId, request.PackageId, request.Quantity);

        // 1. Validate venue (giống SubmitVenueToAdminAsync)
        var venue = await _unitOfWork.VenueLocations.GetByIdWithDetailsAsync(venueId);
        
        if (venue == null || venue.IsDeleted == true)
        {
            return new SubmitVenueWithPaymentResponse 
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
            return new SubmitVenueWithPaymentResponse 
            { 
                IsSuccess = false, 
                Message = "Unauthorized access" 
            };
        }

        // 3. Validate venue status
        if (venue.Status != VenueLocationStatus.DRAFTED.ToString())
        {
            return new SubmitVenueWithPaymentResponse 
            { 
                IsSuccess = false, 
                Message = $"Venue status is {venue.Status}, cannot submit." 
            };
        }

        // 4. Validate required fields
        var missingFields = new List<string>();
        if (string.IsNullOrWhiteSpace(venue.Name)) missingFields.Add("Name");
        if (string.IsNullOrWhiteSpace(venue.Description)) missingFields.Add("Description");
        if (string.IsNullOrWhiteSpace(venue.Address)) missingFields.Add("Address");
        
        var coverImages = DeserializeImages(venue.CoverImage);
        if (coverImages == null || !coverImages.Any()) missingFields.Add("CoverImage");
        
        if (string.IsNullOrWhiteSpace(venue.PhoneNumber)) missingFields.Add("Phone Number");
        if (string.IsNullOrWhiteSpace(venue.Email)) missingFields.Add("Email");
        
        if (!venue.VenueLocationTags.Any()) missingFields.Add("LocationTag");
        
        if (venue.Latitude == null || venue.Longitude == null) 
            missingFields.Add("Location Coordinates (Latitude/Longitude)");
        
        if (venue.PriceMin == null || venue.PriceMax == null) 
            missingFields.Add("Price Range (Min/Max)");

        if (missingFields.Any())
        {
            return new SubmitVenueWithPaymentResponse 
            { 
                IsSuccess = false, 
                Message = "Please fill in all required fields before submitting.", 
                MissingFields = missingFields 
            };
        }

        // 5. Validate package
        var package = await _unitOfWork.Context.Set<SubscriptionPackage>()
            .FirstOrDefaultAsync(p => p.Id == request.PackageId 
                && p.IsDeleted != true 
                && p.IsActive == true);

        if (package == null)
        {
            return new SubmitVenueWithPaymentResponse 
            { 
                IsSuccess = false, 
                Message = "Package not found or inactive" 
            };
        }

        if (!string.Equals(package.Type, "VENUE", StringComparison.OrdinalIgnoreCase))
        {
            return new SubmitVenueWithPaymentResponse 
            { 
                IsSuccess = false, 
                Message = "This package is not for venue subscription" 
            };
        }

        if (package.Price == null || package.Price <= 0 || 
            package.DurationDays == null || package.DurationDays <= 0)
        {
            return new SubmitVenueWithPaymentResponse 
            { 
                IsSuccess = false, 
                Message = "Package configuration is invalid" 
            };
        }

        // 6. Check if there's already a pending payment
        var existingPending = await _unitOfWork.Context.Set<VenueSubscriptionPackage>()
            .Where(vsp => vsp.VenueId == venueId 
                && vsp.Status == VenueSubscriptionPackageStatus.PENDING_PAYMENT.ToString()
                && vsp.CreatedAt > DateTime.UtcNow.AddMinutes(-15))
            .FirstOrDefaultAsync();

        if (existingPending != null)
        {
            return new SubmitVenueWithPaymentResponse 
            { 
                IsSuccess = false, 
                Message = "There is already a pending payment for this venue. Please complete or wait for it to expire." 
            };
        }

        // 7. Calculate amount and duration
        var totalAmount = package.Price.Value * request.Quantity;
        var totalDays = package.DurationDays.Value * request.Quantity;

        // 7.5. Validate payment method
        var paymentMethod = request.PaymentMethod?.ToUpper() ?? "VIETQR";
        if (paymentMethod != "VIETQR" && paymentMethod != "WALLET")
        {
            return new SubmitVenueWithPaymentResponse 
            { 
                IsSuccess = false, 
                Message = "Invalid payment method. Must be VIETQR or WALLET" 
            };
        }

        // 7.6. If WALLET, check balance first
        if (paymentMethod == "WALLET")
        {
            var (hasSufficient, currentBalance) = await _walletPaymentService.CheckWalletBalanceAsync(userId, totalAmount);
            if (!hasSufficient)
            {
                return new SubmitVenueWithPaymentResponse 
                { 
                    IsSuccess = false, 
                    Message = $"Insufficient wallet balance. Available: {currentBalance:N0} VND, Required: {totalAmount:N0} VND"
                };
            }
        }

        // 8. Start transaction with SERIALIZABLE isolation (prevent race condition)
        using var dbTransaction = await _unitOfWork.Context.Database.BeginTransactionAsync(
            System.Data.IsolationLevel.Serializable);
        
        try
        {
            // 9. Create VenueSubscriptionPackage (PENDING_PAYMENT)
            var subscription = new VenueSubscriptionPackage
            {
                VenueId = venueId,
                PackageId = request.PackageId,
                Quantity = request.Quantity,
                StartDate = null,
                EndDate = null,
                Status = VenueSubscriptionPackageStatus.PENDING_PAYMENT.ToString(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Context.Set<VenueSubscriptionPackage>().AddAsync(subscription);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("✅ Created subscription ID: {SubId}", subscription.Id);

            // 10. Create Transaction
            var paymentContent = $"VSP{subscription.Id}";
            
            var transaction = new Transaction
            {
                UserId = userId,
                Amount = totalAmount,
                Currency = "VND",
                PaymentMethod = paymentMethod,
                TransType = 1, // VENUE_SUBSCRIPTION
                DocNo = subscription.Id,
                Description = $"Thanh toán gói {package.PackageName} cho {venue.Name} (x{request.Quantity})",
                Status = TransactionStatus.PENDING.ToString(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Context.Set<Transaction>().AddAsync(transaction);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("✅ Created transaction ID: {TxId}, PaymentMethod: {Method}", transaction.Id, paymentMethod);

            // ========== WALLET PAYMENT FLOW ==========
            if (paymentMethod == "WALLET")
            {
                // Process wallet payment immediately
                var walletResult = await _walletPaymentService.ProcessWalletPaymentAsync(
                    userId, 
                    totalAmount, 
                    transaction.Id, 
                    transaction.Description ?? "Venue subscription payment");

                if (!walletResult.IsSuccess)
                {
                    await dbTransaction.RollbackAsync();
                    return new SubmitVenueWithPaymentResponse 
                    { 
                        IsSuccess = false, 
                        Message = walletResult.Message 
                    };
                }

                // Activate subscription immediately
                var now = DateTime.UtcNow;
                subscription.Status = VenueSubscriptionPackageStatus.ACTIVE.ToString();
                subscription.StartDate = now;
                subscription.EndDate = now.AddDays(totalDays);
                subscription.UpdatedAt = now;
                _unitOfWork.Context.Set<VenueSubscriptionPackage>().Update(subscription);

                // Update venue status to PENDING for admin approval
                // Use Attach + modify only specific properties to avoid tracking conflicts
                var venueToUpdate = new VenueLocation { Id = venueId };
                _unitOfWork.Context.Set<VenueLocation>().Attach(venueToUpdate);
                venueToUpdate.Status = VenueLocationStatus.PENDING.ToString();
                venueToUpdate.UpdatedAt = now;
                _unitOfWork.Context.Entry(venueToUpdate).Property(v => v.Status).IsModified = true;
                _unitOfWork.Context.Entry(venueToUpdate).Property(v => v.UpdatedAt).IsModified = true;

                await _unitOfWork.SaveChangesAsync();
                await dbTransaction.CommitAsync();

                _logger.LogInformation("✅ WALLET payment completed - TxId: {TxId}, SubId: {SubId}, VenueStatus: PENDING, Balance: {OldBalance} → {NewBalance}",
                    transaction.Id, subscription.Id, walletResult.OldBalance, walletResult.NewBalance);

                return new SubmitVenueWithPaymentResponse
                {
                    IsSuccess = true,
                    Message = $"Payment successful via Wallet. Venue submitted for admin approval. Balance: {walletResult.OldBalance:N0} → {walletResult.NewBalance:N0} VND",
                    TransactionId = transaction.Id,
                    SubscriptionId = subscription.Id,
                    QrCodeUrl = null, // No QR for wallet payment
                    Amount = totalAmount,
                    BankInfo = null,
                    ExpireAt = null,
                    PaymentContent = paymentContent,
                    PackageName = package.PackageName ?? "Unknown",
                    TotalDays = totalDays,
                    PaymentMethod = "WALLET",
                    WalletBalance = walletResult.NewBalance
                };
            }

            // ========== VIETQR PAYMENT FLOW (ORIGINAL LOGIC) ==========
            // 11. Create Sepay transaction
            SepayTransactionResponse sepayResponse;
            try
            {
                // Order code = VSP{subscriptionId} để tracking
                sepayResponse = await _sepayService.CreateTransactionAsync(
                    totalAmount, 
                    paymentContent, 
                    $"VSP{subscription.Id}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to create Sepay transaction");
                await dbTransaction.RollbackAsync();
                return new SubmitVenueWithPaymentResponse 
                { 
                    IsSuccess = false, 
                    Message = "Unable to create payment transaction. Please try again." 
                };
            }

            if (sepayResponse.Data == null || string.IsNullOrEmpty(sepayResponse.Data.QrCode))
            {
                _logger.LogError("❌ Sepay response invalid - no QR code");
                await dbTransaction.RollbackAsync();
                return new SubmitVenueWithPaymentResponse 
                { 
                    IsSuccess = false, 
                    Message = "Failed to generate QR code. Please try again." 
                };
            }

            // 12. Update transaction with VietQR info
            var expireAt = DateTime.UtcNow.AddMinutes(15);
            var bankInfo = _sepayService.GetBankInfo();
            
            var externalRef = System.Text.Json.JsonSerializer.Serialize(new
            {
                sepayTransactionId = sepayResponse.Data.Id,
                qrCodeUrl = sepayResponse.Data.QrCode, // VietQR image URL
                qrData = sepayResponse.Data.QrData,
                orderCode = sepayResponse.Data.OrderCode,
                expireAt,
                bankInfo = new { bankInfo.BankName, bankInfo.AccountNumber, bankInfo.AccountName }
            });

            transaction.ExternalRefCode = externalRef;
            transaction.UpdatedAt = DateTime.UtcNow;
            
            _unitOfWork.Context.Set<Transaction>().Update(transaction);
            await _unitOfWork.SaveChangesAsync();

            await dbTransaction.CommitAsync();

            _logger.LogInformation("✅ VIETQR payment initiated - TxId: {TxId}, SubId: {SubId}, SepayId: {SepayId}", 
                transaction.Id, subscription.Id, sepayResponse.Data.Id);

            // 13. Return response with QR code
            return new SubmitVenueWithPaymentResponse
            {
                IsSuccess = true,
                Message = "Venue validated successfully. Please complete payment to submit for approval.",
                TransactionId = transaction.Id,
                SubscriptionId = subscription.Id,
                QrCodeUrl = sepayResponse.Data.QrCode, // VietQR image URL
                Amount = totalAmount,
                BankInfo = new BankInfo
                {
                    BankName = bankInfo.BankName,
                    AccountNumber = bankInfo.AccountNumber,
                    AccountName = bankInfo.AccountName
                },
                ExpireAt = expireAt,
                PaymentContent = paymentContent,
                PackageName = package.PackageName ?? "Unknown",
                TotalDays = totalDays,
                PaymentMethod = "VIETQR"
            };
        }
        catch (Exception ex)
        {
            await dbTransaction.RollbackAsync();
            _logger.LogError(ex, "❌ Error in submit with payment");
            return new SubmitVenueWithPaymentResponse 
            { 
                IsSuccess = false, 
                Message = "An error occurred while processing payment. Please try again." 
            };
        }
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
            RejectionDetails = string.IsNullOrWhiteSpace(v.RejectReason) ? null : System.Text.Json.JsonSerializer.Deserialize<List<RejectionRecord>>(v.RejectReason),
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
        var status = request.Status?.ToUpper();
        if (status != VenueLocationStatus.ACTIVE.ToString() && status != VenueLocationStatus.DRAFTED.ToString())
        {
            return new VenueSubmissionResult { IsSuccess = false, Message = "Invalid status. Only 'ACTIVE' or 'DRAFTED' are allowed." };
        }

        var venue = await _unitOfWork.VenueLocations.GetByIdAsync(request.VenueId);
        
        if (venue == null || venue.IsDeleted == true)
        {
            return new VenueSubmissionResult { IsSuccess = false, Message = "Venue not found" };
        }

        if (venue.Status != VenueLocationStatus.PENDING.ToString())
        {
             return new VenueSubmissionResult { IsSuccess = false, Message = $"Cannot approve/reject venue with status '{venue.Status}'. Only 'PENDING' venues can be processed." };
        }

        if (status == VenueLocationStatus.DRAFTED.ToString() && string.IsNullOrWhiteSpace(request.Reason))
        {
            return new VenueSubmissionResult { IsSuccess = false, Message = "Reason is required when rejecting venue to DRAFTED status." };
        }

        using var dbTransaction = await _unitOfWork.Context.Database.BeginTransactionAsync();
        
        try
        {
            venue.Status = status;
            venue.UpdatedAt = DateTime.UtcNow;
            
            // Lưu rejection details vào JSONB khi reject
            if (status == VenueLocationStatus.DRAFTED.ToString() && !string.IsNullOrWhiteSpace(request.Reason))
            {
                // Parse existing rejections hoặc tạo mới
                var existingRejections = new List<RejectionRecord>();
                if (!string.IsNullOrWhiteSpace(venue.RejectReason))
                {
                    try
                    {
                        existingRejections = System.Text.Json.JsonSerializer.Deserialize<List<RejectionRecord>>(venue.RejectReason) ?? new List<RejectionRecord>();
                    }
                    catch
                    {
                        // Nếu parse fail, bắt đầu mới
                    }
                }
                
                // Thêm rejection mới vào list
                existingRejections.Add(new RejectionRecord
                {
                    Reason = request.Reason,
                    RejectedAt = DateTime.UtcNow.ToString("o"),
                    RejectedBy = "ADMIN"
                });
                
                // Serialize array trực tiếp
                venue.RejectReason = System.Text.Json.JsonSerializer.Serialize(existingRejections);
                _logger.LogInformation("Saved rejection details for venue {VenueId}: Total {Count} rejections", venue.Id, existingRejections.Count);
            }
            else if (status == VenueLocationStatus.ACTIVE.ToString())
            {
                // Clear rejection details khi approve
                venue.RejectReason = null;
            }
            
            if (venue.CreatedAt.HasValue && venue.CreatedAt.Value.Kind == DateTimeKind.Unspecified)
                venue.CreatedAt = DateTime.SpecifyKind(venue.CreatedAt.Value, DateTimeKind.Utc);

            string refundMessage = "";

            if (status == VenueLocationStatus.DRAFTED.ToString())
            {
                // Tìm subscription với status ACTIVE (không phải COMPLETED)
                var activeSubscription = await _unitOfWork.Context.Set<VenueSubscriptionPackage>()
                    .Include(vsp => vsp.Package)
                    .FirstOrDefaultAsync(vsp => vsp.VenueId == request.VenueId 
                        && vsp.Status == VenueSubscriptionPackageStatus.ACTIVE.ToString());

                if (activeSubscription != null)
                {
                    var successfulTransaction = await _unitOfWork.Context.Set<Transaction>()
                        .FirstOrDefaultAsync(t => t.TransType == (int)TransactionType.VENUE_SUBSCRIPTION 
                            && t.DocNo == activeSubscription.Id
                            && t.Status == TransactionStatus.SUCCESS.ToString());

                    if (successfulTransaction != null && successfulTransaction.Amount > 0)
                    {
                        var venueOwner = await _unitOfWork.Context.Set<VenueOwnerProfile>()
                            .FirstOrDefaultAsync(vop => vop.Id == venue.VenueOwnerId);

                        if (venueOwner != null)
                        {
                            var refundMetadata = new Dictionary<string, object>
                            {
                                { "venueId", venue.Id },
                                { "venueName", venue.Name ?? "" },
                                { "rejectionReason", request.Reason ?? "Venue rejected by admin" }
                            };

                            var refundResult = await _refundService.ProcessRefundAsync(
                                userId: venueOwner.UserId,
                                amount: successfulTransaction.Amount,
                                transType: (int)TransactionType.VENUE_SUBSCRIPTION,
                                docNo: activeSubscription.Id,
                                reason: $"Địa điểm '{venue.Name}' bị từ chối. Lý do: {request.Reason}",
                                originalTransactionId: successfulTransaction.Id,
                                metadata: refundMetadata
                            );

                            if (refundResult.IsSuccess)
                            {
                                activeSubscription.Status = VenueSubscriptionPackageStatus.REFUNDED.ToString();
                                activeSubscription.UpdatedAt = DateTime.UtcNow;
                                _unitOfWork.Context.Set<VenueSubscriptionPackage>().Update(activeSubscription);

                                refundMessage = $" Đã hoàn {refundResult.RefundAmount:N0} VND vào ví (Balance: {refundResult.OldBalance:N0} → {refundResult.NewBalance:N0} VND).";
                                
                                // Gửi email thông báo hoàn tiền
                                try
                                {
                                    var emailHtml = EmailRefundTemplate.GetVenueRefundEmailContent(
                                        venueOwner.BusinessName ?? "Venue Owner",
                                        venue.Name ?? "Địa điểm",
                                        activeSubscription.Package?.PackageName ?? "Gói đăng ký",
                                        refundResult.RefundAmount,
                                        refundResult.OldBalance,
                                        refundResult.NewBalance,
                                        request.Reason ?? "Không đạt yêu cầu"
                                    );

                                    var emailRequest = new capstone_backend.Business.DTOs.Email.SendEmailRequest
                                    {
                                        To = "lehuyvuok@gmail.com",
                                        Subject = $"[CoupleMood] Thông báo hoàn tiền - Địa điểm {venue.Name} bị từ chối",
                                        HtmlBody = emailHtml,
                                        FromName = "CoupleMood"
                                    };

                                    if (!string.IsNullOrWhiteSpace(emailRequest.To))
                                    {
                                        var emailSent = await _emailService.SendEmailAsync(emailRequest);
                                        if (emailSent)
                                        {
                                            _logger.LogInformation("✅ Sent refund notification email to {Email}", emailRequest.To);
                                        }
                                        else
                                        {
                                            _logger.LogWarning("⚠️ Failed to send refund email to {Email}", emailRequest.To);
                                        }
                                    }
                                }
                                catch (Exception emailEx)
                                {
                                    _logger.LogError(emailEx, "❌ Error sending refund notification email");
                                }
                            }
                            else
                            {
                                await dbTransaction.RollbackAsync();
                                return new VenueSubmissionResult 
                                { 
                                    IsSuccess = false, 
                                    Message = $"Failed to process refund: {refundResult.Message}" 
                                };
                            }
                        }
                        else
                        {
                            // Không tìm thấy venue owner, vẫn cancel subscription
                            activeSubscription.Status = VenueSubscriptionPackageStatus.CANCELLED.ToString();
                            activeSubscription.UpdatedAt = DateTime.UtcNow;
                            _unitOfWork.Context.Set<VenueSubscriptionPackage>().Update(activeSubscription);
                            _logger.LogWarning("Venue owner not found for venue {VenueId}, subscription cancelled without refund", venue.Id);
                        }
                    }
                    else
                    {
                        // Không có transaction hoặc amount = 0, chỉ cancel subscription
                        activeSubscription.Status = VenueSubscriptionPackageStatus.CANCELLED.ToString();
                        activeSubscription.UpdatedAt = DateTime.UtcNow;
                        _unitOfWork.Context.Set<VenueSubscriptionPackage>().Update(activeSubscription);
                        _logger.LogInformation("No valid transaction found for subscription {SubId}, status updated to CANCELLED", activeSubscription.Id);
                    }
                }
            }

            _unitOfWork.VenueLocations.Update(venue);
            await _unitOfWork.SaveChangesAsync();
            
            // Tạo STAFF account khi venue được approve
            string staffAccountMessage = "";
            if (status == VenueLocationStatus.ACTIVE.ToString())
            {
                try
                {
                    // Lấy thông tin venue owner
                    var venueOwner = await _unitOfWork.Context.Set<VenueOwnerProfile>()
                        .Include(vop => vop.User)
                        .FirstOrDefaultAsync(vop => vop.Id == venue.VenueOwnerId);

                    if (venueOwner != null)
                    {
                        // Tạo email an toàn cho STAFF (loại bỏ ký tự đặc biệt)
                        var businessNameSafe = System.Text.RegularExpressions.Regex.Replace(
                            venueOwner.BusinessName?.ToLower() ?? "venue", 
                            @"[^a-z0-9]", 
                            ""
                        );
                        if (string.IsNullOrEmpty(businessNameSafe))
                            businessNameSafe = "venue";
                        
                        var staffEmail = $"staff.venue{venue.Id}.{businessNameSafe}@system.com";
                        
                        // Kiểm tra email đã tồn tại chưa
                        var existingUser = await _unitOfWork.Context.Set<UserAccount>()
                            .FirstOrDefaultAsync(u => u.Email == staffEmail);
                        
                        if (existingUser == null)
                        {
                            var staffPassword = GenerateRandomPassword(12);
                            
                            var staffUser = new UserAccount
                            {
                                Email = staffEmail,
                                PasswordHash = BCrypt.Net.BCrypt.HashPassword(staffPassword),
                                DisplayName = $"Staff - {venue.Name}",
                                PhoneNumber = venue.PhoneNumber,
                                Role = "STAFF",
                                IsActive = true,
                                IsVerified = true,
                                IsDeleted = false,
                                AssignedVenueLocationId = venue.Id,
                                CreatedAt = DateTime.UtcNow,
                                UpdatedAt = DateTime.UtcNow
                            };

                            await _unitOfWork.Context.Set<UserAccount>().AddAsync(staffUser);
                            await _unitOfWork.SaveChangesAsync();

                            _logger.LogInformation("✅ Created STAFF account for venue {VenueId}: Email={Email}, UserId={UserId}, AssignedVenueLocationId={AssignedVenueLocationId}", 
                                venue.Id, staffEmail, staffUser.Id, staffUser.AssignedVenueLocationId);

                            // Gửi email thông tin STAFF account cho venue owner
                            try
                            {
                                var emailHtml = EmailAccountInfoTemplate.GetStaffAccountInfoEmailContent(
                                    venueOwner.BusinessName ?? "Venue Owner",
                                    venue.Name,
                                    staffEmail,
                                    staffPassword
                                );

                                var emailRequest = new capstone_backend.Business.DTOs.Email.SendEmailRequest
                                {
                                    To = "lehuyvuok@gmail.com",
                                    Subject = $"[CoupleMood] Địa điểm {venue.Name} đã được phê duyệt - Thông tin tài khoản STAFF",
                                    HtmlBody = emailHtml,
                                    FromName = "CoupleMood"
                                };

                                if (!string.IsNullOrWhiteSpace(emailRequest.To))
                                {
                                    var emailSent = await _emailService.SendEmailAsync(emailRequest);
                                    if (emailSent)
                                    {
                                        _logger.LogInformation("✅ Sent STAFF account info email to {Email}", emailRequest.To);
                                        staffAccountMessage = $" | STAFF account created & email sent";
                                    }
                                    else
                                    {
                                        _logger.LogWarning("⚠️ Failed to send email to {Email}", emailRequest.To);
                                        staffAccountMessage = $" | STAFF account created but email failed";
                                    }
                                }
                                else
                                {
                                    _logger.LogWarning("⚠️ Venue owner has no email address");
                                    staffAccountMessage = $" | STAFF account created but no owner email";
                                }
                            }
                            catch (Exception emailEx)
                            {
                                _logger.LogError(emailEx, "❌ Error sending STAFF account email");
                                staffAccountMessage = $" | STAFF account created but email error";
                            }
                        }
                        else
                        {
                            // Update AssignedVenueLocationId nếu staff account đã tồn tại
                            if (existingUser.AssignedVenueLocationId != venue.Id)
                            {
                                existingUser.AssignedVenueLocationId = venue.Id;
                                existingUser.UpdatedAt = DateTime.UtcNow;
                                _unitOfWork.Context.Set<UserAccount>().Update(existingUser);
                                await _unitOfWork.SaveChangesAsync();
                                
                                _logger.LogInformation("✅ Updated STAFF account for venue {VenueId}: Email={Email}, AssignedVenueLocationId={AssignedVenueLocationId}", 
                                    venue.Id, staffEmail, existingUser.AssignedVenueLocationId);
                                staffAccountMessage = $" | STAFF account updated: {staffEmail}";
                            }
                            else
                            {
                                _logger.LogInformation("ℹ️ STAFF account already exists for venue {VenueId}: {Email}", venue.Id, staffEmail);
                                staffAccountMessage = $" | STAFF account already exists: {staffEmail}";
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Failed to create STAFF account for venue {VenueId}", venue.Id);
                    staffAccountMessage = " | Failed to create STAFF account (check logs)";
                    // Không rollback transaction, chỉ log lỗi
                }
            }
            
            await dbTransaction.CommitAsync();

            if (status == VenueLocationStatus.ACTIVE.ToString())
            {
                try
                {
                    await _meilisearchService.IndexVenueLocationAsync(request.VenueId);
                }
                catch
                {
                }
            }

            return new VenueSubmissionResult 
            { 
                IsSuccess = true, 
                Message = $"Venue {status} successfully{refundMessage}{staffAccountMessage}" 
            };
        }
        catch
        {
            await dbTransaction.RollbackAsync();
            return new VenueSubmissionResult 
            { 
                IsSuccess = false, 
                Message = "Failed to process venue approval/rejection" 
            };
        }
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

    /// <summary>
    /// Get venue statistics for debugging search functionality
    /// </summary>
    public async Task<(int Total, int Active, int Pending, int Drafted, int Deleted, Dictionary<string, int> StatusBreakdown)> GetAllVenuesForStatsAsync()
    {
        _logger.LogInformation("Getting venue statistics for search debugging");

        var allVenues = await _unitOfWork.VenueLocations.GetAllAsync();
        
        var total = allVenues.Count();
        var active = allVenues.Count(v => v.Status == VenueLocationStatus.ACTIVE.ToString() && v.IsDeleted != true);
        var pending = allVenues.Count(v => v.Status == VenueLocationStatus.PENDING.ToString() && v.IsDeleted != true);
        var drafted = allVenues.Count(v => v.Status == VenueLocationStatus.DRAFTED.ToString() && v.IsDeleted != true);
        var deleted = allVenues.Count(v => v.IsDeleted == true);

        var statusBreakdown = allVenues
            .Where(v => v.IsDeleted != true)
            .GroupBy(v => v.Status ?? "NULL")
            .ToDictionary(g => g.Key, g => g.Count());

        _logger.LogInformation("Stats: Total={Total}, Active={Active}, Pending={Pending}, Drafted={Drafted}, Deleted={Deleted}", 
            total, active, pending, drafted, deleted);

        return (total, active, pending, drafted, deleted, statusBreakdown);
    }

    /// <summary>
    /// Get venue location with KYC documents and venue owner profile
    /// Query directly from DbContext to get full citizen information from UserAccount
    /// </summary>
    public async Task<VenueLocationWithKycResponse?> GetVenueLocationWithKycAsync(int venueId)
    {
        // Query trực tiếp từ DbContext để lấy đầy đủ thông tin citizen từ UserAccount
        var venue = await _unitOfWork.Context.Set<VenueLocation>()
            .Include(v => v.VenueOwner)
                .ThenInclude(vo => vo.User)
            .FirstOrDefaultAsync(v => v.Id == venueId && v.IsDeleted != true);

        if (venue == null)
        {
            _logger.LogWarning("Venue location with ID {VenueId} not found or deleted", venueId);
            return null;
        }

        var response = new VenueLocationWithKycResponse
        {
            Id = venue.Id,
            Name = venue.Name,
            WebsiteUrl = venue.WebsiteUrl,
            Status = venue.Status ?? VenueLocationStatus.DRAFTED.ToString(),
            Address = venue.VenueOwner.Address,
            BusinessLicenseUrl = venue.BusinessLicenseUrl,
            VenueOwner = new VenueOwnerKycInfo
            {
                Id = venue.VenueOwner.Id,
                BusinessName = venue.VenueOwner.BusinessName,
                PhoneNumber = venue.VenueOwner.PhoneNumber,
                Email = venue.VenueOwner.Email,
                // Lấy citizen documents từ UserAccount
                CitizenIdFrontUrl = venue.VenueOwner.User?.CitizenIdFrontUrl,
                CitizenIdBackUrl = venue.VenueOwner.User?.CitizenIdBackUrl,
                BusinessLicenseUrl = venue.VenueOwner.User?.BusinessLicenseUrl
            }
        };

        _logger.LogInformation("Retrieved venue location with KYC for ID {VenueId}", venueId);
        return response;
    }

    /// <summary>
    /// Generate random password for STAFF account
    /// </summary>
    private string GenerateRandomPassword(int length)
    {
        const string upperChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        const string lowerChars = "abcdefghijklmnopqrstuvwxyz";
        const string digitChars = "0123456789";
        const string specialChars = "!@#$%^&*";
        const string allChars = upperChars + lowerChars + digitChars + specialChars;

        var random = new Random();
        var password = new char[length];

        // Đảm bảo có ít nhất 1 ký tự mỗi loại
        password[0] = upperChars[random.Next(upperChars.Length)];
        password[1] = lowerChars[random.Next(lowerChars.Length)];
        password[2] = digitChars[random.Next(digitChars.Length)];
        password[3] = specialChars[random.Next(specialChars.Length)];

        // Fill phần còn lại với random characters
        for (int i = 4; i < length; i++)
        {
            password[i] = allChars[random.Next(allChars.Length)];
        }

        // Shuffle password
        return new string(password.OrderBy(x => random.Next()).ToArray());
    }

    /// <summary>
    /// Update venue opening hours for all days of the week
    /// </summary>
    public async Task<bool> UpdateVenueOpeningHoursAsync(UpdateVenueOpeningHoursRequest request)
    {
        _logger.LogInformation("Updating opening hours for venue {VenueId}", request.VenueLocationId);

        var venue = await _unitOfWork.VenueLocations.GetByIdAsync(request.VenueLocationId);
        if (venue == null || venue.IsDeleted == true)
        {
            _logger.LogWarning("Venue {VenueId} not found or deleted", request.VenueLocationId);
            return false;
        }

        // Get existing opening hours
        var existingHours = await _unitOfWork.Context.Set<VenueOpeningHour>()
            .Where(oh => oh.VenueLocationId == request.VenueLocationId)
            .ToListAsync();

        // Delete all existing hours
        _unitOfWork.Context.Set<VenueOpeningHour>().RemoveRange(existingHours);

        // Add new opening hours
        foreach (var hourDto in request.OpeningHours)
        {
            var openingHour = new VenueOpeningHour
            {
                VenueLocationId = request.VenueLocationId,
                Day = hourDto.Day,
                OpenTime = hourDto.IsClosed ? TimeSpan.Zero : TimeSpan.Parse(hourDto.OpenTime!),
                CloseTime = hourDto.IsClosed ? TimeSpan.Zero : TimeSpan.Parse(hourDto.CloseTime!),
                IsClosed = hourDto.IsClosed
            };

            await _unitOfWork.Context.Set<VenueOpeningHour>().AddAsync(openingHour);
        }

        await _unitOfWork.SaveChangesAsync();
        _logger.LogInformation("Successfully updated opening hours for venue {VenueId}", request.VenueLocationId);

        return true;
    }
}
