using capstone_backend.Business.DTOs.Recommendation;
using OpenAI.Chat;
using System.Text.Json;

namespace capstone_backend.Business.Recommendation;

/// <summary>
/// Parses natural language queries to extract intent, mood, personality tags, and region
/// Static helper class for AI-powered query parsing
/// </summary>
public static class QueryParser
{
    /// <summary>
    /// Context extracted from parsed natural language query
    /// </summary>
    public class ParsedQueryContext
    {
        public string Intent { get; set; } = "";
        public string? DetectedMood { get; set; }
        public List<string> DetectedPersonalityTags { get; set; } = new();
        public string? DetectedRegion { get; set; }
    }

    /// <summary>
    /// Parses natural language query using AI
    /// </summary>
    public static async Task<ParsedQueryContext> ParseQueryWithAIAsync(
        RecommendationRequest request,
        ChatClient chatClient,
        ILogger logger)
    {
        var context = new ParsedQueryContext();

        // Skip if query is empty or too short
        if (string.IsNullOrWhiteSpace(request.Query) || request.Query.Length < 5)
        {
            return context;
        }

        try
        {
            var systemPrompt = BuildParsingSystemPrompt();
            var userPrompt = $"Câu hỏi: \"{request.Query}\"";

            var messages = new List<ChatMessage>
            {
                new SystemChatMessage(systemPrompt),
                new UserChatMessage(userPrompt)
            };

            // Add timeout for parsing
            using var cts = new CancellationTokenSource(2000); // Max 2s for parsing
            var chatCompletion = await chatClient.CompleteChatAsync(messages, cancellationToken: cts.Token);
            
            var responseText = chatCompletion.Value.Content[0].Text;
            logger.LogInformation($"[AI Parsing] Raw response: {responseText}");

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

            logger.LogInformation("Parsed query: Intent={Intent}, Mood={Mood}, Tags={Tags}",
                context.Intent, context.DetectedMood, string.Join(",", context.DetectedPersonalityTags));
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to parse query with AI, continuing with structured data");
        }

        return context;
    }

    /// <summary>
    /// Builds the system prompt for query parsing
    /// </summary>
    private static string BuildParsingSystemPrompt()
    {
        return @"Bạn là trợ lý phân tích ngôn ngữ tự nhiên cho hệ thống gợi ý địa điểm hẹn hò.
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
    }
}
