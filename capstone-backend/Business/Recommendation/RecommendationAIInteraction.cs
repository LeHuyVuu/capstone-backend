using OpenAI.Chat;
using System.Text;

namespace capstone_backend.Business.Recommendation;

/// <summary>
/// Class này chịu trách nhiệm gửi danh sách địa điểm + ngữ cảnh (context) lên cho AI
/// và nhận về lời giải thích "Tại sao địa điểm này phù hợp?".
/// </summary>
public static class RecommendationAIInteraction
{
    /// <summary>
    /// Gọi ChatGPT để lấy lời giải thích cho từng địa điểm
    /// </summary>
    public static async Task<Dictionary<int, string>> GetExplanationsAsync(
        ChatClient chatClient,
        ILogger logger,
        string venueContextStr,
        string? coupleMoodType,
        List<string> personalityTags,
        string? mbti1,
        string? mbti2,
        string? userQuery)
    {
        // 1. Tạo System Prompt (Hướng dẫn đóng vai)
        var systemPrompt = BuildSystemPrompt();

        // 2. Tạo User Prompt (Dữ liệu đầu vào)
        var userPrompt = BuildUserPrompt(venueContextStr, coupleMoodType, personalityTags, mbti1, mbti2, userQuery);

        var messages = new List<ChatMessage>
        {
            new SystemChatMessage(systemPrompt),
            new UserChatMessage(userPrompt)
        };

        try
        {
            // 3. Gọi AI (Timeout 3s để nhanh)
            using var cts = new CancellationTokenSource(3000); 
            var response = await chatClient.CompleteChatAsync(messages, cancellationToken: cts.Token);
            var content = response.Value.Content[0].Text;

            // 4. Parse kết quả trả về
            return ParseAIResponse(content);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Lỗi khi gọi OpenAI lấy explanation (hoặc timeout). Sẽ dùng lý do mặc định.");
            return new Dictionary<int, string>();
        }
    }

    // --- Private Helpers ---

    private static string BuildSystemPrompt()
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

    private static string BuildUserPrompt(
        string context, 
        string? mood, 
        List<string> tags, 
        string? mbti1, 
        string? mbti2, 
        string? query)
    {
        var sb = new StringBuilder();

        if (!string.IsNullOrEmpty(query))
        {
            sb.AppendLine($"=== USER REQUEST ===\n\"{query}\"\n");
        }

        sb.AppendLine("=== COUPLE PROFILE ===");
        if (!string.IsNullOrEmpty(mbti1)) sb.AppendLine($"MBTI 1: {mbti1}");
        if (!string.IsNullOrEmpty(mbti2)) sb.AppendLine($"MBTI 2: {mbti2}");
        if (!string.IsNullOrEmpty(mood)) sb.AppendLine($"Couple Mood: {mood}");
        if (tags.Any()) sb.AppendLine($"Tags: {string.Join(", ", tags)}");
        
        sb.AppendLine("\n" + context);

        return sb.ToString();
    }

    private static Dictionary<int, string> ParseAIResponse(string aiContent)
    {
        var result = new Dictionary<int, string>();
        var lines = aiContent.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            
            // Lấy Overview
            if (trimmed.StartsWith("OVERVIEW:", StringComparison.OrdinalIgnoreCase))
            {
                result[-1] = trimmed.Substring("OVERVIEW:".Length).Trim();
                continue;
            }

            // Lấy từng item [1], [2]...
            if (trimmed.StartsWith("[") && trimmed.Contains("]"))
            {
                var endIndex = trimmed.IndexOf(']');
                // Parse số trong ngoặc: [1] -> 1
                if (endIndex > 0 && int.TryParse(trimmed.Substring(1, endIndex - 1), out int index))
                {
                    // Lưu vào dictionary với key = index - 1 (để map với list 0-based)
                    result[index - 1] = trimmed.Substring(endIndex + 1).Trim();
                }
            }
        }
        return result;
    }
}
