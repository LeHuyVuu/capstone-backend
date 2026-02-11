using capstone_backend.Business.DTOs.Recommendation;
using capstone_backend.Business.DTOs.Common;
using capstone_backend.Business.Interfaces;
using capstone_backend.Business.Recommendation;
using capstone_backend.Data.Interfaces;
using System.Diagnostics;
using OpenAI.Chat;

namespace capstone_backend.Business.Services;

/// <summary>
/// AI-powered recommendation service using OpenAI GPT-4o-mini with RAG pattern
/// Orchestrates the recommendation flow by delegating to specialized helpers
/// </summary>
public class OpenAIRecommendationService : IRecommendationService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMoodMappingService _moodMapping;
    private readonly IPersonalityMappingService _personalityMapping;
    private readonly ChatClient _chatClient;
    private readonly ILogger<OpenAIRecommendationService> _logger;

    public OpenAIRecommendationService(
        IUnitOfWork unitOfWork,
        IMoodMappingService moodMapping,
        IPersonalityMappingService personalityMapping,
        IConfiguration configuration,
        ILogger<OpenAIRecommendationService> logger)
    {
        _unitOfWork = unitOfWork;
        _moodMapping = moodMapping;
        _personalityMapping = personalityMapping;
        _logger = logger;

        var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY") 
            ?? throw new InvalidOperationException("OPENAI_API_KEY environment variable not configured");
        var modelName = Environment.GetEnvironmentVariable("MODEL_NAME") ?? "gpt-4o-mini";

        _logger.LogInformation($"[OpenAI] Using model: {modelName}");
        _chatClient = new ChatClient(model: modelName, apiKey: apiKey);
    }

    /// <summary>
    /// Gets AI-powered venue recommendations based on flexible input
    /// </summary>
    public async Task<RecommendationResponse> GetRecommendationsAsync(RecommendationRequest request)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            // Phase 1: Parallel - Parse query với AI và lấy couple mood
            var parsedContextTask = !string.IsNullOrWhiteSpace(request.Query) 
                ? QueryParser.ParseQueryWithAIAsync(request, _chatClient, _logger) 
                : Task.FromResult(new QueryParser.ParsedQueryContext());

            string? coupleMoodType = null;
            if (request.MoodId.HasValue && request.PartnerMoodId.HasValue)
            {
                coupleMoodType = await _moodMapping.GetCoupleMoodTypeAsync(
                    request.MoodId.Value,
                    request.PartnerMoodId.Value
                );
            }
            else if (request.MoodId.HasValue)
            {
                var mood = await _unitOfWork.MoodTypes.GetByIdAsync(request.MoodId.Value);
                coupleMoodType = mood?.Name;
            }

            var parsedContext = await parsedContextTask;

            // Merge AI-parsed mood
            if (!string.IsNullOrEmpty(parsedContext.DetectedMood))
            {
                coupleMoodType ??= parsedContext.DetectedMood;
            }

            // Phase 2: Map MBTI to personality tags
            var personalityTags = new List<string>();
            if (!string.IsNullOrEmpty(request.MbtiType))
            {
                personalityTags = _personalityMapping.GetPersonalityTags(
                    request.MbtiType,
                    request.PartnerMbtiType ?? request.MbtiType
                );
            }

            // Merge AI-parsed personality tags
            if (parsedContext.DetectedPersonalityTags.Any())
            {
                personalityTags.AddRange(parsedContext.DetectedPersonalityTags);
                personalityTags = personalityTags.Distinct().ToList();
            }

            var searchArea = request.Area ?? parsedContext.DetectedRegion;

            // Xác định single person: không có PartnerMoodId và PartnerMbtiType
            bool isSinglePerson = !request.PartnerMoodId.HasValue && string.IsNullOrEmpty(request.PartnerMbtiType);

            // Debug logging
            _logger.LogInformation($"[Recommendation] Lat/Lon: {request.Latitude}, {request.Longitude} | Area: {searchArea}");
            _logger.LogInformation($"[Recommendation] Mood: {coupleMoodType} | Tags: {string.Join(", ", personalityTags)} | SinglePerson: {isSinglePerson}");

            // Pagination setup
            var pageSize = request.PageSize;
            pageSize = Math.Min(pageSize, 50); // Max 50 per page
            var page = Math.Max(1, request.Page);
            
            // Fetch exact amount needed for current page
            var totalToFetch = page * pageSize;

            // Phase 3: Query venues trực tiếp từ repo
            var hasFilters = !string.IsNullOrEmpty(coupleMoodType) || personalityTags.Any();
            
            // Khi single person: truyền mood vào singleMoodName để filter bằng DetailTag.Contains
            // Khi couple: truyền mood vào coupleMoodType để filter bằng CoupleMoodType.Name
            string? coupleMoodForFilter = isSinglePerson ? null : (hasFilters ? coupleMoodType : null);
            string? singleMoodForFilter = isSinglePerson && hasFilters ? coupleMoodType : null;
            
            var (venuesWithDistance, totalCount) = await _unitOfWork.VenueLocations.GetForRecommendationsAsync(
                coupleMoodForFilter,
                hasFilters ? personalityTags : new List<string>(),
                singleMoodForFilter,
                searchArea,
                request.Latitude,
                request.Longitude,
                request.RadiusKm,
                totalToFetch,
                request.BudgetLevel
            );
            var venues = venuesWithDistance.Select(x => x.Venue).ToList();
            var distanceMap = venuesWithDistance.ToDictionary(x => x.Venue.Id, x => x.DistanceKm);

            // Fallback: nếu có filter mà không đủ kết quả, query thêm không filter
            if (hasFilters && venues.Count < totalToFetch)
            {
                var (additionalVenuesWithDistance, _) = await _unitOfWork.VenueLocations.GetForRecommendationsAsync(
                    null,
                    new List<string>(),
                    null,
                    searchArea,
                    request.Latitude,
                    request.Longitude,
                    request.RadiusKm,
                    totalToFetch - venues.Count,
                    request.BudgetLevel
                );

                var venueIds = new HashSet<int>(venues.Select(v => v.Id));
                foreach (var (venue, distanceKm) in additionalVenuesWithDistance)
                {
                    if (venueIds.Add(venue.Id))
                    {
                        venues.Add(venue);
                        distanceMap[venue.Id] = distanceKm;
                    }
                }
            }

            if (!venues.Any())
            {
                return new RecommendationResponse
                {
                    Recommendations = new PagedResult<RecommendedVenue>
                    {
                        Items = new List<RecommendedVenue>(),
                        PageNumber = page,
                        PageSize = pageSize,
                        TotalCount = 0
                    },
                    Explanation = "Xin lỗi, hiện tại chúng tôi chưa có đủ dữ liệu địa điểm. Vui lòng thử lại sau.",
                    CoupleMoodType = isSinglePerson ? null : coupleMoodType,
                    SingleMood = isSinglePerson ? coupleMoodType : null,
                    PersonalityTags = personalityTags,
                    ProcessingTimeMs = stopwatch.ElapsedMilliseconds
                };
            }
     

          
            // Phase 5: Build response with pagination
            var allRecommendations = RecommendationFormatter.FormatRecommendedVenues(venues, distanceMap);
            
            // Apply pagination
            var pagedItems = allRecommendations
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();
            
            stopwatch.Stop();

            return new RecommendationResponse
            {
                Recommendations = new PagedResult<RecommendedVenue>
                {
                    Items = pagedItems,
                    PageNumber = page,
                    PageSize = pageSize,
                    TotalCount = totalCount  // Use actual total count from repository
                },
                CoupleMoodType = isSinglePerson ? null : coupleMoodType,
                SingleMood = isSinglePerson ? coupleMoodType : null,
                PersonalityTags = personalityTags,
                ProcessingTimeMs = stopwatch.ElapsedMilliseconds
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating recommendations");
            stopwatch.Stop();
            return await GetFallbackRecommendationsAsync(request, stopwatch.ElapsedMilliseconds);
        }
    }

    /// <summary>
    /// Fallback recommendations khi main flow fails
    /// </summary>
    private async Task<RecommendationResponse> GetFallbackRecommendationsAsync(RecommendationRequest request, long processingTimeMs)
    {
        var pageSize = Math.Min(request.PageSize, 50);
        var page = Math.Max(1, request.Page);
        var totalToFetch = page * pageSize;
        
        try
        {
            var (venuesWithDistance, totalCount) = await _unitOfWork.VenueLocations.GetForRecommendationsAsync(
                null, 
                new List<string>(), 
                null,
                request.Area, 
                request.Latitude, 
                request.Longitude, 
                request.RadiusKm, 
                totalToFetch,
                request.BudgetLevel
            );
            var venues = venuesWithDistance.Select(x => x.Venue).ToList();
            var distanceMap = venuesWithDistance.ToDictionary(x => x.Venue.Id, x => x.DistanceKm);

            if (!venues.Any())
            {
                return new RecommendationResponse
                {
                    Recommendations = new PagedResult<RecommendedVenue>
                    {
                        Items = new List<RecommendedVenue>(),
                        PageNumber = page,
                        PageSize = pageSize,
                        TotalCount = 0
                    },
                    Explanation = "Xin lỗi, hiện tại hệ thống gặp sự cố. Vui lòng thử lại sau.",
                    ProcessingTimeMs = processingTimeMs
                };
            }

            var allRecommendations = RecommendationFormatter.FormatFallbackVenues(venues, totalToFetch, distanceMap);
            var pagedItems = allRecommendations
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return new RecommendationResponse
            {
                Recommendations = new PagedResult<RecommendedVenue>
                {
                    Items = pagedItems,
                    PageNumber = page,
                    PageSize = pageSize,
                    TotalCount = totalCount  // Use actual total count from repository
                },
                Explanation = "Đây là những địa điểm phổ biến và được yêu thích nhất.",
                ProcessingTimeMs = processingTimeMs
            };
        }
        catch
        {
            return new RecommendationResponse
            {
                Recommendations = new PagedResult<RecommendedVenue>
                {
                    Items = new List<RecommendedVenue>(),
                    PageNumber = page,
                    PageSize = pageSize,
                    TotalCount = 0
                },
                Explanation = "Hệ thống tạm thời không khả dụng. Vui lòng thử lại sau.",
                ProcessingTimeMs = processingTimeMs
            };
        }
    }
}
