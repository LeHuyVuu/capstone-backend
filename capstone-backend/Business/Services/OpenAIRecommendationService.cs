using capstone_backend.Business.DTOs.Recommendation;
using capstone_backend.Business.Interfaces;
using capstone_backend.Business.Recommendation;
using capstone_backend.Data.Interfaces;
using Microsoft.EntityFrameworkCore;
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
    private readonly IVenueScoringEngine _scoringEngine;
    private readonly ChatClient _chatClient;
    private readonly ILogger<OpenAIRecommendationService> _logger;

    public OpenAIRecommendationService(
        IUnitOfWork unitOfWork,
        IMoodMappingService moodMapping,
        IPersonalityMappingService personalityMapping,
        IVenueScoringEngine scoringEngine,
        IConfiguration configuration,
        ILogger<OpenAIRecommendationService> logger)
    {
        _unitOfWork = unitOfWork;
        _moodMapping = moodMapping;
        _personalityMapping = personalityMapping;
        _scoringEngine = scoringEngine;
        _logger = logger;

        // Đọc từ environment variables (.env file) thay vì appsettings
        var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY") 
            ?? throw new InvalidOperationException("OPENAI_API_KEY environment variable not configured");
        var modelName = Environment.GetEnvironmentVariable("MODEL_NAME") ?? "gpt-4o-mini";

        _logger.LogInformation($"[OpenAI] Using model: {modelName}");
        _logger.LogInformation($"[OpenAI] API Key: {apiKey.Substring(0, Math.Min(15, apiKey.Length))}...");

        _chatClient = new ChatClient(model: modelName, apiKey: apiKey);
    }

    /// <summary>
    /// Gets AI-powered venue recommendations based on flexible input
    /// Handles: natural language queries, structured data, or any combination
    /// Always returns recommendations even with minimal information
    /// </summary>
    public async Task<RecommendationResponse> GetRecommendationsAsync(RecommendationRequest request)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            // Phase 1-2: Parallel execution for AI parsing and mood lookup
            var parsedContextTask = !string.IsNullOrWhiteSpace(request.Query) 
                ? QueryParser.ParseQueryWithAIAsync(request, _chatClient, _logger) 
                : Task.FromResult(new QueryParser.ParsedQueryContext());

            var coupleMoodTypeTask = GetCoupleMoodTypeAsync(request);

            // Wait for both to complete
            await Task.WhenAll(parsedContextTask, coupleMoodTypeTask);
            
            var parsedContext = await parsedContextTask;
            string? coupleMoodType = await coupleMoodTypeTask;

            // Merge AI-parsed mood with structured data
            if (!string.IsNullOrEmpty(parsedContext.DetectedMood))
            {
                coupleMoodType ??= parsedContext.DetectedMood;
            }

            // Phase 3: Map MBTI to personality tags (if MBTI provided)
            var personalityTags = new List<string>();
            if (!string.IsNullOrEmpty(request.MbtiType))
            {
                if (!string.IsNullOrEmpty(request.PartnerMbtiType))
                {
                    personalityTags = _personalityMapping.GetPersonalityTags(
                        request.MbtiType,
                        request.PartnerMbtiType
                    );
                }
                else
                {
                    personalityTags = _personalityMapping.GetPersonalityTags(
                        request.MbtiType,
                        request.MbtiType
                    );
                }
            }

            // Merge AI-parsed personality tags
            if (parsedContext.DetectedPersonalityTags.Any())
            {
                personalityTags.AddRange(parsedContext.DetectedPersonalityTags);
                personalityTags = personalityTags.Distinct().ToList();
            }

            // Use AI-detected region if not explicitly provided
            var searchRegion = request.Region ?? parsedContext.DetectedRegion;
            
            // Debug logging
            _logger.LogInformation($"[Recommendation] Region from request: {request.Region}");
            _logger.LogInformation($"[Recommendation] Lat/Lon: {request.Latitude}, {request.Longitude}");
            _logger.LogInformation($"[Recommendation] Region from AI: {parsedContext.DetectedRegion}");
            _logger.LogInformation($"[Recommendation] Final search region: {searchRegion}");
            _logger.LogInformation($"[Recommendation] Couple mood: {coupleMoodType}");
            _logger.LogInformation($"[Recommendation] Personality tags: {string.Join(", ", personalityTags)}");

            // Phase 4: Smart venue retrieval with intelligent fallback
            var venues = await RetrieveCandidateVenuesSmartAsync(
                coupleMoodType,
                personalityTags,
                searchRegion,
                request.Latitude,
                request.Longitude,
                request.RadiusKm,
                request.Limit * 3 // Get more candidates for better filtering
            );

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

            // Phase 5: Score and rank venues
            var rankedVenues = _scoringEngine.RankVenues(
                venues,
                coupleMoodType,
                personalityTags,
                request.MbtiType,
                request.PartnerMbtiType,
                request.BudgetLevel,
                request.Limit
            );

            // Phase 6: Build context for OpenAI
            var context = VenueContextBuilder.BuildVenueContext(
                rankedVenues,
                coupleMoodType,
                personalityTags,
                request.Query,
                parsedContext);

            // Phase 7: Call OpenAI for match reasoning (with timeout protection)
            var aiExplanationsTask = GetAIExplanationsWithTimeoutAsync(
                context,
                coupleMoodType,
                personalityTags,
                request.MbtiType,
                request.PartnerMbtiType,
                request.Query,
                timeoutMs: 3000 // Max 3s for AI call
            );
            
            var aiExplanations = await aiExplanationsTask;

            // Phase 8: Build final response
            var recommendations = RecommendationFormatter.FormatRecommendedVenues(rankedVenues, aiExplanations);

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

            // Even on error, try to return something useful
            return await GetFallbackRecommendationsAsync(request, stopwatch.ElapsedMilliseconds);
        }
    }

    /// <summary>
    /// Smart venue retrieval with single optimized query
    /// </summary>
    private async Task<List<Data.Entities.VenueLocation>> RetrieveCandidateVenuesSmartAsync(
        string? coupleMoodType,
        List<string> personalityTags,
        string? region,
        decimal? latitude,
        decimal? longitude,
        decimal? radiusKm,
        int limit)
    {
        // Single optimized query with tiered filtering
        var hasFilters = !string.IsNullOrEmpty(coupleMoodType) || personalityTags.Any();
        
        if (hasFilters)
        {
            // Try with filters first
            var venues = await _unitOfWork.VenueLocations.GetForRecommendationsAsync(
                coupleMoodType,
                personalityTags,
                region,
                latitude,
                longitude,
                radiusKm,
                limit
            );

            // If we got enough, return immediately
            if (venues.Count >= limit / 2)
            {
                return venues;
            }

            // Otherwise, get more without filters and combine
            var additionalVenues = await _unitOfWork.VenueLocations.GetForRecommendationsAsync(
                null,
                new List<string>(),
                region,
                latitude,
                longitude,
                radiusKm,
                limit - venues.Count
            );

            // Efficient deduplication using HashSet
            var venueIds = new HashSet<int>(venues.Select(v => v.Id));
            foreach (var venue in additionalVenues)
            {
                if (venueIds.Add(venue.Id))
                {
                    venues.Add(venue);
                }
            }

            return venues;
        }

        // No filters - direct query for top venues
        return await _unitOfWork.VenueLocations.GetForRecommendationsAsync(
            null,
            new List<string>(),
            region,
            latitude,
            longitude,
            radiusKm,
            limit
        );
    }

    /// <summary>
    /// Get couple mood type (extracted for parallel execution)
    /// </summary>
    private async Task<string?> GetCoupleMoodTypeAsync(RecommendationRequest request)
    {
        if (request.MoodId.HasValue && request.PartnerMoodId.HasValue)
        {
            return await _moodMapping.GetCoupleMoodTypeAsync(
                request.MoodId.Value,
                request.PartnerMoodId.Value
            );
        }
        
        if (request.MoodId.HasValue)
        {
            // Single user - use their mood directly
            var mood = await _unitOfWork.Context.Set<Data.Entities.MoodType>()
                .AsNoTracking() // Performance: read-only query
                .FirstOrDefaultAsync(m => m.Id == request.MoodId.Value);
            return mood?.Name;
        }

        return null;
    }

    /// <summary>
    /// Calls OpenAI with timeout protection and AI-powered explanations
    /// </summary>
    private async Task<Dictionary<int, string>> GetAIExplanationsWithTimeoutAsync(
        string venueContext,
        string? coupleMoodType,
        List<string> personalityTags,
        string? mbti1,
        string? mbti2,
        string? userQuery,
        int timeoutMs = 3000)
    {
        var systemPrompt = PromptBuilder.BuildSystemPrompt();
        var userPrompt = PromptBuilder.BuildUserPrompt(
            venueContext,
            coupleMoodType,
            personalityTags,
            mbti1,
            mbti2,
            userQuery);

        var messages = new List<ChatMessage>
        {
            new SystemChatMessage(systemPrompt),
            new UserChatMessage(userPrompt)
        };

        try
        {
            using var cts = new CancellationTokenSource(timeoutMs);
            var response = await _chatClient.CompleteChatAsync(messages, cancellationToken: cts.Token);
            var content = response.Value.Content[0].Text;

            return ResponseFormatter.ParseAIResponse(content);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("OpenAI API call timed out after {TimeoutMs}ms, using fallback explanations", timeoutMs);
            return new Dictionary<int, string>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling OpenAI API");
            return new Dictionary<int, string>();
        }
    }

    /// <summary>
    /// Fallback recommendations when main flow fails
    /// </summary>
    private async Task<RecommendationResponse> GetFallbackRecommendationsAsync(RecommendationRequest request, long processingTimeMs)
    {
        try
        {
            // Get top-rated venues regardless of filters
            var venues = await _unitOfWork.VenueLocations.GetForRecommendationsAsync(
                null, new List<string>(), request.Region, request.Latitude, request.Longitude, request.RadiusKm, request.Limit
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

            var recommendations = RecommendationFormatter.FormatFallbackVenues(venues, request.Limit);

            return new RecommendationResponse
            {
                Recommendations = recommendations,
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

