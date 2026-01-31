using capstone_backend.Business.DTOs.Recommendation;
using capstone_backend.Data.Entities;

namespace capstone_backend.Business.Recommendation;

/// <summary>
/// Formats recommendation responses with venue details
/// Static helper class for response building
/// </summary>
public static class RecommendationFormatter
{
    /// <summary>
    /// Formats venues into recommended venue objects with AI explanations
    /// </summary>
    public static List<RecommendedVenue> FormatRecommendedVenues(
        List<VenueLocation> venues,
        Dictionary<int, string> aiExplanations)
    {
        return venues.Select((venue, index) => new RecommendedVenue
        {
            VenueLocationId = venue.Id,
            Name = venue.Name,
            Address = venue.Address,
            Description = venue.Description ?? "",
            MatchReason = aiExplanations.ContainsKey(index) 
                ? aiExplanations[index] 
                : "Phù hợp với sở thích của bạn",
            AverageRating = venue.Reviews?.Any() == true
                ? (decimal)venue.Reviews.Where(r => r.Rating.HasValue).Average(r => (double)r.Rating!.Value)
                : null,
            ReviewCount = venue.Reviews?.Count ?? 0,
            CoverImage = venue.CoverImage,
            InteriorImage = venue.InteriorImage,
            FullPageMenuImage = venue.FullPageMenuImage,
            MatchedTags = venue.LocationTag != null
                ? new List<string> 
                { 
                    venue.LocationTag.CoupleMoodType?.Name!,
                    venue.LocationTag.CouplePersonalityType?.Name!
                }.Where(name => !string.IsNullOrEmpty(name)).ToList()
                : new List<string>()
        }).ToList();
    }

    /// <summary>
    /// Formats fallback venues when main recommendation logic fails
    /// </summary>
    public static List<RecommendedVenue> FormatFallbackVenues(
        List<VenueLocation> venues,
        int limit)
    {
        return venues.Take(limit).Select(venue => new RecommendedVenue
        {
            VenueLocationId = venue.Id,
            Name = venue.Name,
            Address = venue.Address,
            Description = venue.Description ?? "",
            MatchReason = "Địa điểm phổ biến và được đánh giá cao",
            AverageRating = venue.Reviews?.Any() == true
                ? (decimal)venue.Reviews.Where(r => r.Rating.HasValue).Average(r => (double)r.Rating!.Value)
                : null,
            ReviewCount = venue.Reviews?.Count ?? 0,
            CoverImage = venue.CoverImage,
            InteriorImage = venue.InteriorImage,
            FullPageMenuImage = venue.FullPageMenuImage,
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
