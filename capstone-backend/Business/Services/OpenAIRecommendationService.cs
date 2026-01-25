using capstone_backend.Business.DTOs.Recommendation;
using capstone_backend.Business.Interfaces;
using capstone_backend.Data.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using OpenAI.Chat;

namespace capstone_backend.Business.Services;

/// <summary>
/// AI-powered recommendation service using OpenAI GPT-4o-mini with RAG pattern
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
                ? ParseQueryWithAIAsync(request) 
                : Task.FromResult(new ParsedQueryContext());

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
            var context = BuildVenueContext(rankedVenues, coupleMoodType, personalityTags, request.Query, parsedContext);

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
            var recommendations = rankedVenues.Select((rv, index) =>
            {
                var venue = rv.venue;
                return new RecommendedVenue
                {
                    VenueLocationId = venue.Id,
                    Name = venue.Name,
                    Address = venue.Address,
                    Description = venue.Description ?? "",
                    Score = Math.Round(rv.score, 2),
                    MatchReason = aiExplanations.ContainsKey(index) 
                        ? aiExplanations[index] 
                        : "Phù hợp với sở thích của bạn",
                    AverageRating = venue.Reviews?.Any() == true
                        ? (decimal)venue.Reviews.Where(r => r.Rating.HasValue).Average(r => (double)r.Rating!.Value)
                        : null,
                    ReviewCount = venue.Reviews?.Count ?? 0,
                    Images = new List<string>(),
                    MatchedTags = venue.LocationTag != null
                        ? new List<string> 
                        { 
                            venue.LocationTag.CoupleMoodType?.Name!,
                            venue.LocationTag.CouplePersonalityType?.Name!
                        }.Where(name => !string.IsNullOrEmpty(name)).ToList()
                        : new List<string>()
                };
            }).ToList();

            stopwatch.Stop();

            return new RecommendationResponse
            {
                Recommendations = recommendations,
                Explanation = aiExplanations.ContainsKey(-1) 
                    ? aiExplanations[-1] 
                    : GenerateDefaultExplanation(coupleMoodType, personalityTags, request.Query, parsedContext),
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
    /// Builds context string for OpenAI from venues
    /// </summary>
    private string BuildVenueContext(
        List<(Data.Entities.VenueLocation venue, double score)> rankedVenues,
        string? coupleMoodType,
        List<string> personalityTags,
        string? userQuery,
        ParsedQueryContext parsedContext)
    {
        var sb = new StringBuilder();
        sb.AppendLine("=== DANH SÁCH ĐỊA ĐIỂM ===");

        if (!string.IsNullOrEmpty(userQuery))
        {
            sb.AppendLine($"YÊU CẦU CỦA NGƯỜI DÙNG: \"{userQuery}\"");
            if (!string.IsNullOrEmpty(parsedContext.Intent))
            {
                sb.AppendLine($"Ý ĐỊNH PHÁT HIỆN: {parsedContext.Intent}");
            }
            sb.AppendLine();
        }

        for (int i = 0; i < rankedVenues.Count; i++)
        {
            var (venue, score) = rankedVenues[i];
            sb.AppendLine($"\n[{i + 1}] {venue.Name}");
            sb.AppendLine($"Địa chỉ: {venue.Address}");
            sb.AppendLine($"Mô tả: {venue.Description}");
            sb.AppendLine($"Điểm phù hợp: {score:F2}/100");

            var tags = new List<string>();
            if (venue.LocationTag?.CoupleMoodType?.Name != null)
                tags.Add(venue.LocationTag.CoupleMoodType.Name);
            if (venue.LocationTag?.CouplePersonalityType?.Name != null)
                tags.Add(venue.LocationTag.CouplePersonalityType.Name);

            if (tags.Any())
            {
                sb.AppendLine($"Tags: {string.Join(", ", tags)}");
            }

            var avgRating = venue.Reviews?.Any() == true
                ? (decimal?)venue.Reviews.Where(r => r.Rating.HasValue).Average(r => (double)r.Rating!.Value)
                : null;

            if (avgRating.HasValue)
            {
                sb.AppendLine($"Đánh giá: {avgRating:F1}⭐ ({venue.Reviews!.Count} reviews)");
            }
        }

        return sb.ToString();
    }

    /// <summary>
    /// Calls OpenAI with timeout protection
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
        var systemPrompt = BuildSystemPrompt();
        var userPrompt = BuildUserPrompt(venueContext, coupleMoodType, personalityTags, mbti1, mbti2, userQuery);

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

            return ParseAIResponse(content);
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
    /// Builds system prompt for OpenAI
    /// </summary>
    private string BuildSystemPrompt()
    {
        return @"Bạn là một chuyên gia tư vấn địa điểm hẹn hò và giải trí tại Việt Nam. 
Nhiệm vụ của bạn là phân tích danh sách địa điểm đã được chấm điểm và giải thích tại sao mỗi địa điểm phù hợp với cặp đôi dựa trên:
- Tính cách MBTI của họ
- Trạng thái cảm xúc hiện tại (mood)
- Loại mood cặp đôi (couple mood type)
- Các đặc điểm tính cách (personality tags)

Hãy trả lời theo định dạng:
OVERVIEW: [Tổng quan về gợi ý - 2-3 câu]

[1] [Lý do địa điểm 1 phù hợp - 1-2 câu ngắn gọn]
[2] [Lý do địa điểm 2 phù hợp - 1-2 câu ngắn gọn]
...

Giữ câu trả lời ngắn gọn, thân thiện, và tập trung vào lý do phù hợp.";
    }

    /// <summary>
    /// Builds user prompt for OpenAI
    /// </summary>
    private string BuildUserPrompt(
        string venueContext,
        string? coupleMoodType,
        List<string> personalityTags,
        string? mbti1,
        string? mbti2,
        string? userQuery)
    {
        var sb = new StringBuilder();
        
        if (!string.IsNullOrEmpty(userQuery))
        {
            sb.AppendLine($"=== YÊU CẦU CỦA NGƯỜI DÙNG ===");
            sb.AppendLine($"\"{userQuery}\"");
            sb.AppendLine();
        }

        sb.AppendLine("=== THÔNG TIN CẶP ĐÔI ===");

        if (!string.IsNullOrEmpty(mbti1))
        {
            sb.AppendLine($"MBTI người 1: {mbti1}");
        }

        if (!string.IsNullOrEmpty(mbti2))
        {
            sb.AppendLine($"MBTI người 2: {mbti2}");
        }

        if (!string.IsNullOrEmpty(coupleMoodType))
        {
            sb.AppendLine($"Couple Mood: {coupleMoodType}");
        }

        if (personalityTags.Any())
        {
            sb.AppendLine($"Personality Tags: {string.Join(", ", personalityTags)}");
        }

        sb.AppendLine();
        sb.AppendLine(venueContext);

        return sb.ToString();
    }

    /// <summary>
    /// Parses AI response into explanations dictionary
    /// </summary>
    private Dictionary<int, string> ParseAIResponse(string content)
    {
        var result = new Dictionary<int, string>();
        var lines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        string? overview = null;
        foreach (var line in lines)
        {
            var trimmed = line.Trim();

            // Extract overview
            if (trimmed.StartsWith("OVERVIEW:", StringComparison.OrdinalIgnoreCase))
            {
                overview = trimmed.Substring("OVERVIEW:".Length).Trim();
                continue;
            }

            // Extract individual venue explanations [1], [2], etc.
            if (trimmed.StartsWith("[") && trimmed.Contains("]"))
            {
                var endIndex = trimmed.IndexOf(']');
                if (endIndex > 0 && int.TryParse(trimmed.Substring(1, endIndex - 1), out int venueIndex))
                {
                    var explanation = trimmed.Substring(endIndex + 1).Trim();
                    result[venueIndex - 1] = explanation; // Convert to 0-based index
                }
            }
        }

        // Store overview with key -1
        if (!string.IsNullOrEmpty(overview))
        {
            result[-1] = overview;
        }

        return result;
    }

    /// <summary>
    /// Generates default explanation when AI is unavailable
    /// </summary>
    private string GenerateDefaultExplanation(string? coupleMoodType, List<string> personalityTags, string? query, ParsedQueryContext parsedContext)
    {
        var sb = new StringBuilder("Dựa trên phân tích của chúng tôi");

        if (!string.IsNullOrEmpty(query))
        {
            sb.Append($" và yêu cầu \"{query}\" của bạn");
        }

        if (!string.IsNullOrEmpty(coupleMoodType))
        {
            sb.Append($", với trạng thái {coupleMoodType}");
        }

        if (personalityTags.Any())
        {
            sb.Append($" và các đặc điểm {string.Join(", ", personalityTags)}");
        }

        sb.Append(", đây là những địa điểm phù hợp nhất cho bạn.");

        return sb.ToString();
    }

    /// <summary>
    /// Context from natural language query parsing
    /// </summary>
    private class ParsedQueryContext
    {
        public string Intent { get; set; } = "";
        public string? DetectedMood { get; set; }
        public List<string> DetectedPersonalityTags { get; set; } = new();
        public string? DetectedRegion { get; set; }
    }

    /// <summary>
    /// Parses natural language query using AI
    /// </summary>
    private async Task<ParsedQueryContext> ParseQueryWithAIAsync(RecommendationRequest request)
    {
        var context = new ParsedQueryContext();

        // Skip if query is empty or too short
        if (string.IsNullOrWhiteSpace(request.Query) || request.Query.Length < 5)
        {
            return context;
        }

        try
        {
            var systemPrompt = @"Bạn là trợ lý phân tích ngôn ngữ tự nhiên cho hệ thống gợi ý địa điểm hẹn hò.
Nhiệm vụ: Phân tích câu hỏi/yêu cầu của người dùng và trích xuất thông tin theo định dạng JSON.

Trả về JSON với cấu trúc:
{
  ""intent"": ""<mô tả ý định>"",
  ""mood"": ""<tâm trạng nếu có>"",
  ""personalityTags"": [""<tag1>"", ""<tag2>""],
  ""region"": ""<khu vực nếu có>""
}

Các tâm trạng có thể: Hạnh phúc, Lãng mạn, Phiêu lưu, An ủi, Thư giãn, Vui vẻ, Suy ngẫm, Năng động
Các personality tags: Năng động, Yên tĩnh, Sáng tạo, Lãng mạn, Tự phát
Khu vực: Hà Nội, TP.HCM, Đà Nẵng, etc.

Ví dụ:
Input: ""hôm nay anniversary thì đi đâu""
Output: {""intent"": ""Tìm địa điểm kỷ niệm"", ""mood"": ""Lãng mạn"", ""personalityTags"": [""Lãng mạn""], ""region"": null}";

            var userPrompt = $"Câu hỏi: \"{request.Query}\"";

            var messages = new List<ChatMessage>
            {
                new SystemChatMessage(systemPrompt),
                new UserChatMessage(userPrompt)
            };

            // Add timeout for parsing
            using var cts = new CancellationTokenSource(2000); // Max 2s for parsing
            var chatCompletion = await _chatClient.CompleteChatAsync(messages, cancellationToken: cts.Token);
            
            var responseText = chatCompletion.Value.Content[0].Text;
            _logger.LogInformation($"[AI Parsing] Raw response: {responseText}");

            // Parse JSON response
            using var jsonDoc = JsonDocument.Parse(responseText);
            var root = jsonDoc.RootElement;

            if (root.TryGetProperty("intent", out var intentProp))
                context.Intent = intentProp.GetString() ?? "";

            if (root.TryGetProperty("mood", out var moodProp) && moodProp.ValueKind != JsonValueKind.Null)
                context.DetectedMood = moodProp.GetString();

            if (root.TryGetProperty("region", out var regionProp) && regionProp.ValueKind != JsonValueKind.Null)
                context.DetectedRegion = regionProp.GetString();

            if (root.TryGetProperty("personalityTags", out var tagsProp) && tagsProp.ValueKind == JsonValueKind.Array)
            {
                context.DetectedPersonalityTags = tagsProp.EnumerateArray()
                    .Select(t => t.GetString())
                    .Where(t => !string.IsNullOrEmpty(t))
                    .Select(t => t!)
                    .ToList();
            }

            _logger.LogInformation("Parsed query: Intent={Intent}, Mood={Mood}, Tags={Tags}",
                context.Intent, context.DetectedMood, string.Join(",", context.DetectedPersonalityTags));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse query with AI, continuing with structured data");
        }

        return context;
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

            var recommendations = venues.Take(request.Limit).Select(venue => new RecommendedVenue
            {
                VenueLocationId = venue.Id,
                Name = venue.Name,
                Address = venue.Address,
                Description = venue.Description ?? "",
                Score = 70.0,
                MatchReason = "Địa điểm phổ biến và được đánh giá cao",
                AverageRating = venue.Reviews?.Any() == true
                    ? (decimal)venue.Reviews.Where(r => r.Rating.HasValue).Average(r => (double)r.Rating!.Value)
                    : null,
                ReviewCount = venue.Reviews?.Count ?? 0,
                Images = new List<string>(),
                MatchedTags = venue.LocationTag != null
                    ? new List<string>
                    {
                        venue.LocationTag.CoupleMoodType?.Name!,
                        venue.LocationTag.CouplePersonalityType?.Name!
                    }.Where(name => !string.IsNullOrEmpty(name)).ToList()
                    : new List<string>()
            }).ToList();

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

