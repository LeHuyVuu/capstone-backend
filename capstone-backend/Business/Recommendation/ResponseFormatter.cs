namespace capstone_backend.Business.Recommendation;

/// <summary>
/// Parses and formats responses from OpenAI API
/// Static helper class for response handling
/// </summary>
public static class ResponseFormatter
{
    /// <summary>
    /// Parses AI response into explanations dictionary
    /// </summary>
    public static Dictionary<int, string> ParseAIResponse(string content)
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
    public static string GenerateDefaultExplanation(
        string? coupleMoodType,
        List<string> personalityTags,
        string? query,
        QueryParser.ParsedQueryContext parsedContext)
    {
        var sb = new System.Text.StringBuilder("Dựa trên phân tích của chúng tôi");

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
}
