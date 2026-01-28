using System.Text;

namespace capstone_backend.Business.Recommendation;

/// <summary>
/// Builds venue context for OpenAI from ranked venues
/// Static helper class for context construction
/// </summary>
public static class VenueContextBuilder
{
    /// <summary>
    /// Builds context string for OpenAI from venues
    /// </summary>
    public static string BuildVenueContext(
        List<(Data.Entities.VenueLocation venue, double score)> rankedVenues,
        string? coupleMoodType,
        List<string> personalityTags,
        string? userQuery,
        QueryParser.ParsedQueryContext parsedContext)
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
}
