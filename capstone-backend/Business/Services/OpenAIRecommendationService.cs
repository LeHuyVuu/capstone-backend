using capstone_backend.Business.DTOs.Recommendation;
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

            // Debug logging
            _logger.LogInformation($"[Recommendation] Lat/Lon: {request.Latitude}, {request.Longitude} | Area: {searchArea}");
            _logger.LogInformation($"[Recommendation] Mood: {coupleMoodType} | Tags: {string.Join(", ", personalityTags)}");

            // Phase 3: Query venues trực tiếp từ repo
            var hasFilters = !string.IsNullOrEmpty(coupleMoodType) || personalityTags.Any();
            var venues = await _unitOfWork.VenueLocations.GetForRecommendationsAsync(
                hasFilters ? coupleMoodType : null,
                hasFilters ? personalityTags : new List<string>(),
                searchArea,
                request.Latitude,
                request.Longitude,
                request.RadiusKm,
                request.Limit
            );

            // Fallback: nếu có filter mà không đủ kết quả, query thêm không filter
            if (hasFilters && venues.Count < request.Limit)
            {
                var additionalVenues = await _unitOfWork.VenueLocations.GetForRecommendationsAsync(
                    null,
                    new List<string>(),
                    searchArea,
                    request.Latitude,
                    request.Longitude,
                    request.RadiusKm,
                    request.Limit - venues.Count
                );

                var venueIds = new HashSet<int>(venues.Select(v => v.Id));
                foreach (var venue in additionalVenues)
                {
                    if (venueIds.Add(venue.Id))
                    {
                        venues.Add(venue);
                    }
                }
            }

            if (!venues.Any())
            {
                return new RecommendationResponse
                {
                    Recommendations = new List<RecommendedVenue>(),
                    Explanation = "Xin lỗi, hiện tại chúng tôi chưa có đủ dữ liệu địa điểm. Vui lòng thử lại sau.",
                    CoupleMoodType = coupleMoodType,
                    PersonalityTags = personalityTags,
                    ProcessingTimeMs = stopwatch.ElapsedMilliseconds
                };
            }

            // Phase 4: Build context và gọi OpenAI lấy explanations
            var context = VenueContextBuilder.BuildVenueContext(
                venues,
                coupleMoodType,
                personalityTags,
                request.Query,
                parsedContext);

            // Sử dụng RecommendationAIInteraction để gọi AI
            var aiExplanations = await RecommendationAIInteraction.GetExplanationsAsync(
                _chatClient,
                _logger,
                context,
                coupleMoodType,
                personalityTags,
                request.MbtiType,
                request.PartnerMbtiType,
                request.Query
            );

            // Phase 5: Build response
            var recommendations = RecommendationFormatter.FormatRecommendedVenues(venues, aiExplanations);
            stopwatch.Stop();

            return new RecommendationResponse
            {
                Recommendations = recommendations,
                Explanation = aiExplanations.ContainsKey(-1) 
                    ? aiExplanations[-1] 
                    : ResponseFormatter.GenerateDefaultExplanation(coupleMoodType, personalityTags, request.Query, parsedContext),
                CoupleMoodType = coupleMoodType,
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
        try
        {
            var venues = await _unitOfWork.VenueLocations.GetForRecommendationsAsync(
                null, 
                new List<string>(), 
                request.Area, 
                request.Latitude, 
                request.Longitude, 
                request.RadiusKm, 
                request.Limit
            );

            if (!venues.Any())
            {
                return new RecommendationResponse
                {
                    Recommendations = new List<RecommendedVenue>(),
                    Explanation = "Xin lỗi, hiện tại hệ thống gặp sự cố. Vui lòng thử lại sau.",
                    ProcessingTimeMs = processingTimeMs
                };
            }

            return new RecommendationResponse
            {
                Recommendations = RecommendationFormatter.FormatFallbackVenues(venues, request.Limit),
                Explanation = "Đây là những địa điểm phổ biến và được yêu thích nhất.",
                ProcessingTimeMs = processingTimeMs
            };
        }
        catch
        {
            return new RecommendationResponse
            {
                Recommendations = new List<RecommendedVenue>(),
                Explanation = "Hệ thống tạm thời không khả dụng. Vui lòng thử lại sau.",
                ProcessingTimeMs = processingTimeMs
            };
        }
    }
}
