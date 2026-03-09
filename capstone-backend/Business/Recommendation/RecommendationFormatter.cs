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
        Dictionary<int, decimal?> distanceMap)
    {
        return venues.Select((venue, index) => new RecommendedVenue
        {
            VenueLocationId = venue.Id,
            LocationTagId = venue.VenueLocationTags.FirstOrDefault(vlt => vlt.IsDeleted != true)?.LocationTagId,
            VenueOwnerId = venue.VenueOwnerId,
            Name = venue.Name,
            Address = venue.Address,
            Description = venue.Description,
            Email = venue.Email,
            PhoneNumber = venue.PhoneNumber,
            WebsiteUrl = venue.WebsiteUrl,
            PriceMin = venue.PriceMin,
            PriceMax = venue.PriceMax,
            Latitude = venue.Latitude,
            Longitude = venue.Longitude,
            Area = venue.Area,
            AvarageCost = venue.AvarageCost,
            Category = venue.Category,
            Distance = distanceMap.TryGetValue(venue.Id, out var dist) ? dist : null,
            DistanceText = FormatDistanceText(distanceMap.TryGetValue(venue.Id, out var d) ? d : null),
            AverageRating = venue.AverageRating,
            ReviewCount = venue.ReviewCount ?? 0,
            CoverImage = DeserializeImages(venue.CoverImage),
            InteriorImage = DeserializeImages(venue.InteriorImage),
            FullPageMenuImage = DeserializeImages(venue.FullPageMenuImage),
            MatchedTags = venue.VenueLocationTags
                .Where(vlt => vlt.LocationTag != null && vlt.IsDeleted != true)
                .SelectMany(vlt => new[] 
                { 
                    vlt.LocationTag.CoupleMoodType?.Name,
                    vlt.LocationTag.CouplePersonalityType?.Name
                })
                .Where(name => !string.IsNullOrEmpty(name))
                .Distinct()
                .ToList()!
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
            LocationTagId = venue.VenueLocationTags.FirstOrDefault(vlt => vlt.IsDeleted != true)?.LocationTagId,
            VenueOwnerId = venue.VenueOwnerId,
            Name = venue.Name,
            Address = venue.Address,
            Description = venue.Description,
            Email = venue.Email,
            PhoneNumber = venue.PhoneNumber,
            WebsiteUrl = venue.WebsiteUrl,
            PriceMin = venue.PriceMin,
            PriceMax = venue.PriceMax,
            Latitude = venue.Latitude,
            Longitude = venue.Longitude,
            Area = venue.Area,
            AvarageCost = venue.AvarageCost,
            Category = venue.Category,
            Distance = distanceMap.TryGetValue(venue.Id, out var dist) ? dist : null,
            DistanceText = FormatDistanceText(distanceMap.TryGetValue(venue.Id, out var d) ? d : null),
            MatchReason = "Địa điểm phổ biến và được đánh giá cao",
            AverageRating = venue.AverageRating,
            ReviewCount = venue.ReviewCount ?? 0,
            CoverImage = DeserializeImages(venue.CoverImage),
            InteriorImage = DeserializeImages(venue.InteriorImage),
            FullPageMenuImage = DeserializeImages(venue.FullPageMenuImage),
            MatchedTags = venue.VenueLocationTags
                .Where(vlt => vlt.LocationTag != null && vlt.IsDeleted != true)
                .SelectMany(vlt => new[]
                {
                    vlt.LocationTag.CoupleMoodType?.Name,
                    vlt.LocationTag.CouplePersonalityType?.Name
                })
                .Where(name => !string.IsNullOrEmpty(name))
                .Distinct()
                .ToList()!
        }).ToList();
    }

    /// <summary>
    /// Deserialize JSON string to list of image URLs
    /// Handles multiple formats: JSON array, single string, or malformed strings
    /// </summary>
    private static List<string> DeserializeImages(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return new List<string>();

        // Clean up the string - remove surrounding quotes (single or double)
        var cleaned = json.Trim();
        
        // Remove leading/trailing single quotes if present
        if (cleaned.StartsWith("'") && cleaned.EndsWith("'"))
        {
            cleaned = cleaned.Substring(1, cleaned.Length - 2).Trim();
        }
        
        // Remove leading/trailing double quotes if it's a quoted JSON string
        if (cleaned.StartsWith("\"") && cleaned.EndsWith("\"") && cleaned.Length > 2)
        {
            // Only remove if it looks like a quoted JSON array
            if (cleaned.Contains("["))
            {
                cleaned = cleaned.Substring(1, cleaned.Length - 2).Trim();
            }
        }

        try
        {
            // Try to deserialize as JSON array
            if (cleaned.TrimStart().StartsWith("["))
            {
                var result = System.Text.Json.JsonSerializer.Deserialize<List<string>>(cleaned);
                // Filter out empty strings and return
                return result?.Where(s => !string.IsNullOrWhiteSpace(s)).ToList() ?? new List<string>();
            }
            
            // If it's a single URL, return as array with one element
            return new List<string> { cleaned };
        }
        catch (System.Text.Json.JsonException ex)
        {
            // Log the error for debugging
            Console.WriteLine($"[WARNING] Failed to deserialize image JSON: {ex.Message}. Raw value: {json}");
            
            // Try to extract URLs from malformed string
            if (cleaned.Contains("http"))
            {
                // Extract all URLs using basic pattern matching
                var urls = System.Text.RegularExpressions.Regex.Matches(cleaned, @"https?://[^\s,\""'\]]+")
                    .Cast<System.Text.RegularExpressions.Match>()
                    .Select(m => m.Value)
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .ToList();
                
                if (urls.Any())
                    return urls;
            }
            
            // Last resort: return the original string as a single element
            return new List<string> { cleaned };
        }
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
