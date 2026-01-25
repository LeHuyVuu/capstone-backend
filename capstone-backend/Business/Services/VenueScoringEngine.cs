using capstone_backend.Business.DTOs.Recommendation;
using capstone_backend.Data.Entities;

namespace capstone_backend.Business.Services;

/// <summary>
/// Scoring engine for ranking venue recommendations
/// </summary>
public class VenueScoringEngine : IVenueScoringEngine
{
    private readonly IPersonalityMappingService _personalityMapping;

    public VenueScoringEngine(IPersonalityMappingService personalityMapping)
    {
        _personalityMapping = personalityMapping;
    }

    /// <summary>
    /// Calculates overall score for a venue based on multiple factors
    /// Score range: 0-100
    /// </summary>
    public double CalculateScore(
        VenueLocation venue,
        string? coupleMoodType,
        List<string> personalityTags,
        string? mbti1,
        string? mbti2,
        int? budgetLevel)
    {
        double totalScore = 0;
        double maxScore = 0;

        // 1. Mood Match Score (30 points)
        var moodScore = CalculateMoodScore(venue, coupleMoodType);
        totalScore += moodScore;
        maxScore += 30;

        // 2. Personality Match Score (25 points)
        var personalityScore = CalculatePersonalityScore(venue, personalityTags);
        totalScore += personalityScore;
        maxScore += 25;

        // 3. Rating Score (20 points)
        var ratingScore = CalculateRatingScore(venue);
        totalScore += ratingScore;
        maxScore += 20;

        // 4. Popularity Score (15 points)
        var popularityScore = CalculatePopularityScore(venue);
        totalScore += popularityScore;
        maxScore += 15;

        // 5. Budget Match Score (10 points) - if specified
        if (budgetLevel.HasValue)
        {
            var budgetScore = CalculateBudgetScore(venue, budgetLevel.Value);
            totalScore += budgetScore;
            maxScore += 10;
        }

        // Normalize to 0-100 scale
        return maxScore > 0 ? (totalScore / maxScore) * 100 : 0;
    }

    /// <summary>
    /// Calculates mood matching score (0-30)
    /// </summary>
    private double CalculateMoodScore(VenueLocation venue, string? coupleMoodType)
    {
        if (string.IsNullOrEmpty(coupleMoodType))
            return 15; // Neutral score

        // Check if venue has location tag matching the couple mood
        var hasMatchingMood = venue.LocationTag?.CoupleMoodType?.Name.Equals(coupleMoodType, StringComparison.OrdinalIgnoreCase) == true;

        return hasMatchingMood ? 30 : 10;
    }

    /// <summary>
    /// Calculates personality tag matching score (0-25)
    /// </summary>
    private double CalculatePersonalityScore(VenueLocation venue, List<string> personalityTags)
    {
        if (personalityTags == null || !personalityTags.Any())
            return 12; // Neutral score

        // Check if venue has matching personality tag
        var venuePersonalityTag = venue.LocationTag?.CouplePersonalityType?.Name;
        
        if (string.IsNullOrEmpty(venuePersonalityTag))
            return 12; // Neutral score

        var hasMatch = personalityTags.Any(tag =>
            tag.Equals(venuePersonalityTag, StringComparison.OrdinalIgnoreCase)
        );

        return hasMatch ? 25 : 5;
    }

    /// <summary>
    /// Calculates rating-based score (0-20)
    /// </summary>
    private double CalculateRatingScore(VenueLocation venue)
    {
        // Calculate average rating from reviews
        var reviews = venue.Reviews?.Where(r => r.Rating.HasValue).ToList();
        if (reviews == null || !reviews.Any())
            return 10; // Neutral score for no reviews

        var avgRating = reviews.Average(r => (double)r.Rating!.Value);
        
        // Convert 0-5 rating to 0-20 score
        return (avgRating / 5.0) * 20;
    }

    /// <summary>
    /// Calculates popularity score based on review count (0-15)
    /// </summary>
    private double CalculatePopularityScore(VenueLocation venue)
    {
        var reviewCount = venue.Reviews?.Count ?? 0;

        // Logarithmic scale for popularity
        if (reviewCount == 0)
            return 5; // Base score

        if (reviewCount >= 100)
            return 15;
        if (reviewCount >= 50)
            return 13;
        if (reviewCount >= 20)
            return 11;
        if (reviewCount >= 10)
            return 9;
        
        return 7;
    }

    /// <summary>
    /// Calculates budget matching score (0-10)
    /// </summary>
    private double CalculateBudgetScore(VenueLocation venue, int budgetLevel)
    {
        // Estimate venue budget level from price range or other indicators
        // This is simplified - you may want to add actual budget fields to VenueLocation
        
        // For now, assume all venues match moderately
        return 7;
    }

    /// <summary>
    /// Ranks and returns top N venues by score
    /// </summary>
    public List<(VenueLocation venue, double score)> RankVenues(
        List<VenueLocation> venues,
        string? coupleMoodType,
        List<string> personalityTags,
        string? mbti1,
        string? mbti2,
        int? budgetLevel,
        int topN = 10)
    {
        var scoredVenues = venues
            .Select(v => (
                venue: v,
                score: CalculateScore(v, coupleMoodType, personalityTags, mbti1, mbti2, budgetLevel)
            ))
            .OrderByDescending(x => x.score)
            .Take(topN)
            .ToList();

        return scoredVenues;
    }
}

/// <summary>
/// Interface for venue scoring engine
/// </summary>
public interface IVenueScoringEngine
{
    /// <summary>
    /// Calculates overall score for a venue
    /// </summary>
    double CalculateScore(
        VenueLocation venue,
        string? coupleMoodType,
        List<string> personalityTags,
        string? mbti1,
        string? mbti2,
        int? budgetLevel);

    /// <summary>
    /// Ranks venues and returns top N with scores
    /// </summary>
    List<(VenueLocation venue, double score)> RankVenues(
        List<VenueLocation> venues,
        string? coupleMoodType,
        List<string> personalityTags,
        string? mbti1,
        string? mbti2,
        int? budgetLevel,
        int topN = 10);
}
