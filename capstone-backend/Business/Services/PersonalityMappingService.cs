using capstone_backend.Business.Interfaces;

namespace capstone_backend.Business.Services;

/// <summary>
/// Service for mapping MBTI personality types to couple personality tags
/// </summary>
public class PersonalityMappingService : IPersonalityMappingService
{
    // Cache for personality tags to avoid recalculation
    private static readonly Dictionary<string, List<string>> _personalityCache = new();
    private static readonly object _cacheLock = new();

    /// <summary>
    /// Maps two MBTI types to personality tags for couple recommendations
    /// Returns up to 5 personality tags based on MBTI characteristics
    /// </summary>
    public List<string> GetPersonalityTags(string mbti1, string mbti2)
    {
        // Create cache key
        var cacheKey = $"{mbti1?.ToUpper() ?? ""}-{mbti2?.ToUpper() ?? ""}";
        
        // Check cache first
        if (_personalityCache.TryGetValue(cacheKey, out var cachedTags))
        {
            return cachedTags;
        }

        var tags = new HashSet<string>();

        // Analyze both MBTI types (null-safe)
        if (!string.IsNullOrEmpty(mbti1))
            AnalyzeMbtiType(mbti1, tags);
        if (!string.IsNullOrEmpty(mbti2))
            AnalyzeMbtiType(mbti2, tags);

        // Analyze couple compatibility (only if both provided)
        if (!string.IsNullOrEmpty(mbti1) && !string.IsNullOrEmpty(mbti2))
            AnalyzeCoupleDynamics(mbti1, mbti2, tags);

        var result = tags.ToList();
        
        // Store in cache (thread-safe)
        lock (_cacheLock)
        {
            if (!_personalityCache.ContainsKey(cacheKey))
            {
                _personalityCache[cacheKey] = result;
            }
        }

        return result;
    }

    /// <summary>
    /// Analyzes a single MBTI type and adds relevant tags
    /// Maps to 5 database personality types: LÃNG MẠN, VUI VẺ, THƯ THÁI, HÒA GIẢI, PHIÊU LƯU
    /// </summary>
    private void AnalyzeMbtiType(string mbti, HashSet<string> tags)
    {
        if (string.IsNullOrEmpty(mbti) || mbti.Length != 4)
            return;

        mbti = mbti.ToUpper();

        // E/I - Energy orientation
        if (mbti[0] == 'E')
        {
            tags.Add("VUI VẺ"); // Extroverted - Outgoing/Cheerful
        }
        else if (mbti[0] == 'I')
        {
            tags.Add("THƯ THÁI"); // Introverted - Calm/Relaxed
        }

        // S/N - Information gathering
        if (mbti[1] == 'S')
        {
            tags.Add("THƯ THÁI"); // Sensing - Practical, grounded
        }
        else if (mbti[1] == 'N')
        {
            tags.Add("PHIÊU LƯU"); // Intuitive - Adventurous/Exploratory
        }

        // T/F - Decision making
        if (mbti[2] == 'T')
        {
            tags.Add("HÒA GIẢI"); // Thinking - Logical, problem-solving
        }
        else if (mbti[2] == 'F')
        {
            tags.Add("LÃNG MẠN"); // Feeling - Romantic/Emotional
        }

        // J/P - Lifestyle orientation
        if (mbti[3] == 'J')
        {
            tags.Add("THƯ THÁI"); // Judging - Organized, structured
        }
        else if (mbti[3] == 'P')
        {
            tags.Add("PHIÊU LƯU"); // Perceiving - Spontaneous, adventurous
        }
    }

    /// <summary>
    /// Analyzes couple dynamics based on MBTI combination
    /// Maps to 5 database personality types: LÃNG MẠN, VUI VẺ, THƯ THÁI, HÒA GIẢI, PHIÊU LƯU
    /// </summary>
    private void AnalyzeCoupleDynamics(string mbti1, string mbti2, HashSet<string> tags)
    {
        if (string.IsNullOrEmpty(mbti1) || string.IsNullOrEmpty(mbti2))
            return;

        mbti1 = mbti1.ToUpper();
        mbti2 = mbti2.ToUpper();

        // Both extroverted - highly active couple
        if (mbti1[0] == 'E' && mbti2[0] == 'E')
        {
            tags.Add("VUI VẺ");
        }

        // Both introverted - prefer quiet activities
        if (mbti1[0] == 'I' && mbti2[0] == 'I')
        {
            tags.Add("THƯ THÁI");
        }

        // Mix of E and I - balanced activities
        if ((mbti1[0] == 'E' && mbti2[0] == 'I') || (mbti1[0] == 'I' && mbti2[0] == 'E'))
        {
            tags.Add("HÒA GIẢI"); // Need balance/harmony
        }

        // Both feeling types - emotional/romantic couple
        if (mbti1[2] == 'F' && mbti2[2] == 'F')
        {
            tags.Add("LÃNG MẠN");
        }

        // Both perceiving - spontaneous couple
        if (mbti1[3] == 'P' && mbti2[3] == 'P')
        {
            tags.Add("PHIÊU LƯU");
        }

        // Both intuitive - adventurous couple
        if (mbti1[1] == 'N' && mbti2[1] == 'N')
        {
            tags.Add("PHIÊU LƯU");
        }

        // Opposite types in some dimensions - need harmony
        int differences = 0;
        for (int i = 0; i < 4; i++)
        {
            if (mbti1[i] != mbti2[i])
                differences++;
        }

        // If they have 2-3 differences, they need harmony/compromise
        if (differences >= 2 && differences <= 3)
        {
            tags.Add("HÒA GIẢI"); // Need harmony
        }
    }

   
}

