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
        Dictionary<int, string> aiExplanations,
        Dictionary<int, decimal?> distanceMap)
    {
        return venues.Select((venue, index) => new RecommendedVenue
        {
            VenueLocationId = venue.Id,
            LocationTagId = venue.LocationTagId,
            VenueOwnerId = venue.VenueOwnerId,
            Name = venue.Name,
            Address = venue.Address,
            Description = venue.Description ?? "",
            Email = venue.Email,
            PhoneNumber = venue.PhoneNumber,
            WebsiteUrl = venue.WebsiteUrl,
            OpeningTime = venue.OpeningTime,
            ClosingTime = venue.ClosingTime,
            IsOpen = venue.IsOpen,
            PriceMin = venue.PriceMin,
            PriceMax = venue.PriceMax,
            Latitude = venue.Latitude,
            Longitude = venue.Longitude,
            Area = venue.Area,
            AvarageCost = venue.AvarageCost,
            Status = venue.Status,
            Category = venue.Category,
            IsOwnerVerified = venue.IsOwnerVerified,
            CreatedAt = venue.CreatedAt,
            UpdatedAt = venue.UpdatedAt,
            IsDeleted = venue.IsDeleted,
            Distance = distanceMap.TryGetValue(venue.Id, out var dist) ? dist : null,
            DistanceText = FormatDistanceText(distanceMap.TryGetValue(venue.Id, out var d) ? d : null),
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
        int limit,
        Dictionary<int, decimal?> distanceMap)
    {
        return venues.Take(limit).Select(venue => new RecommendedVenue
        {
            VenueLocationId = venue.Id,
            LocationTagId = venue.LocationTagId,
            VenueOwnerId = venue.VenueOwnerId,
            Name = venue.Name,
            Address = venue.Address,
            Description = venue.Description ?? "",
            Email = venue.Email,
            PhoneNumber = venue.PhoneNumber,
            WebsiteUrl = venue.WebsiteUrl,
            OpeningTime = venue.OpeningTime,
            ClosingTime = venue.ClosingTime,
            IsOpen = venue.IsOpen,
            PriceMin = venue.PriceMin,
            PriceMax = venue.PriceMax,
            Latitude = venue.Latitude,
            Longitude = venue.Longitude,
            Area = venue.Area,
            AvarageCost = venue.AvarageCost,
            Status = venue.Status,
            Category = venue.Category,
            IsOwnerVerified = venue.IsOwnerVerified,
            CreatedAt = venue.CreatedAt,
            UpdatedAt = venue.UpdatedAt,
            IsDeleted = venue.IsDeleted,
            Distance = distanceMap.TryGetValue(venue.Id, out var dist) ? dist : null,
            DistanceText = FormatDistanceText(distanceMap.TryGetValue(venue.Id, out var d) ? d : null),
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

    /// <summary>
    /// Formats distance as human-readable text
    /// Under 1km: show in meters (e.g., "500 m")
    /// 1km and above: show in km (e.g., "2.3 km")
    /// </summary>
    private static string? FormatDistanceText(decimal? distanceKm)
    {
        if (!distanceKm.HasValue)
            return null;

        if (distanceKm.Value < 1)
        {
            var meters = (int)(distanceKm.Value * 1000);
            return $"{meters} m";
        }

        return $"{distanceKm.Value:F1} km";
    }
}
