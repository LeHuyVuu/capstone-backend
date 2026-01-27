using System.Text;

namespace capstone_backend.Business.Recommendation;

/// <summary>
/// Builds system and user prompts for OpenAI API calls
/// Static helper class for prompt construction
/// </summary>
public static class PromptBuilder
{
    /// <summary>
    /// Builds system prompt for venue explanation generation
    /// </summary>
    public static string BuildSystemPrompt()
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
    /// Builds user prompt for venue explanation generation
    /// </summary>
    public static string BuildUserPrompt(
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
}
