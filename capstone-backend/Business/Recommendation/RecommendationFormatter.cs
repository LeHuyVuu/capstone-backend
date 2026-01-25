using capstone_backend.Business.DTOs.Recommendation;

namespace capstone_backend.Business.Recommendation;

/// <summary>
/// Formats recommendation responses with venue details
/// Static helper class for response building
/// </summary>
public static class RecommendationFormatter
{
    /// <summary>
    /// Formats ranked venues into recommended venue objects
    /// </summary>
    public static List<RecommendedVenue> FormatRecommendedVenues(
        List<(Data.Entities.VenueLocation venue, double score)> rankedVenues,
        Dictionary<int, string> aiExplanations)
    {
        return rankedVenues.Select((rv, index) =>
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
    }

    /// <summary>
    /// Formats fallback venues when main recommendation logic fails
    /// </summary>
    public static List<RecommendedVenue> FormatFallbackVenues(
        List<Data.Entities.VenueLocation> venues,
        int limit)
    {
        return venues.Take(limit).Select(venue => new RecommendedVenue
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
    }
}
