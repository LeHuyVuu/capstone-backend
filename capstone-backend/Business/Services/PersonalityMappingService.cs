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

        // Analyze both MBTI types
        AnalyzeMbtiType(mbti1, tags);
        AnalyzeMbtiType(mbti2, tags);

        // Analyze couple compatibility
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
    /// </summary>
    private void AnalyzeMbtiType(string mbti, HashSet<string> tags)
    {
        if (string.IsNullOrEmpty(mbti) || mbti.Length != 4)
            return;

        mbti = mbti.ToUpper();

        // E/I - Energy orientation
        if (mbti[0] == 'E')
        {
            tags.Add("Năng động"); // Extroverted - Active/Energetic
        }
        else if (mbti[0] == 'I')
        {
            tags.Add("Yên tĩnh"); // Introverted - Quiet/Peaceful
        }

        // S/N - Information gathering
        if (mbti[1] == 'S')
        {
            // Sensing - Practical, realistic
            // May prefer concrete activities
        }
        else if (mbti[1] == 'N')
        {
            tags.Add("Sáng tạo"); // Intuitive - Creative/Imaginative
        }

        // T/F - Decision making
        if (mbti[2] == 'T')
        {
            // Thinking - Logical
            // May prefer structured activities
        }
        else if (mbti[2] == 'F')
        {
            tags.Add("Lãng mạn"); // Feeling - Romantic/Emotional
        }

        // J/P - Lifestyle orientation
        if (mbti[3] == 'J')
        {
            // Judging - Planned
            // May prefer organized activities
        }
        else if (mbti[3] == 'P')
        {
            tags.Add("Tự phát"); // Perceiving - Spontaneous
        }
    }

    /// <summary>
    /// Analyzes couple dynamics based on MBTI combination
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
            tags.Add("Năng động");
        }

        // Both introverted - prefer quiet activities
        if (mbti1[0] == 'I' && mbti2[0] == 'I')
        {
            tags.Add("Yên tĩnh");
        }

        // Mix of E and I - balanced activities
        if ((mbti1[0] == 'E' && mbti2[0] == 'I') || (mbti1[0] == 'I' && mbti2[0] == 'E'))
        {
            tags.Add("Cân bằng"); // Balanced approach
        }

        // Both feeling types - emotional/romantic couple
        if (mbti1[2] == 'F' && mbti2[2] == 'F')
        {
            tags.Add("Lãng mạn");
        }

        // Both perceiving - spontaneous couple
        if (mbti1[3] == 'P' && mbti2[3] == 'P')
        {
            tags.Add("Tự phát");
        }

        // Both intuitive - creative couple
        if (mbti1[1] == 'N' && mbti2[1] == 'N')
        {
            tags.Add("Sáng tạo");
        }

        // Opposite types in some dimensions - complementary
        int differences = 0;
        for (int i = 0; i < 4; i++)
        {
            if (mbti1[i] != mbti2[i])
                differences++;
        }

        // If they have 2-3 differences, they complement each other well
        if (differences >= 2 && differences <= 3)
        {
            tags.Add("Bổ sung"); // Complementary
        }
    }

    /// <summary>
    /// Gets personality compatibility score (0-100)
    /// </summary>
    public int GetCompatibilityScore(string mbti1, string mbti2)
    {
        if (string.IsNullOrEmpty(mbti1) || string.IsNullOrEmpty(mbti2))
            return 50;

        mbti1 = mbti1.ToUpper();
        mbti2 = mbti2.ToUpper();

        int score = 50; // Base score

        // E/I compatibility - same is good for understanding
        if (mbti1[0] == mbti2[0])
            score += 10;
        else
            score += 5; // Opposite can balance

        // S/N - similar is better for communication
        if (mbti1[1] == mbti2[1])
            score += 15;

        // T/F - opposite can complement
        if (mbti1[2] != mbti2[2])
            score += 10;
        else
            score += 5;

        // J/P - opposite can balance
        if (mbti1[3] != mbti2[3])
            score += 10;
        else
            score += 5;

        return Math.Min(score, 100);
    }
}

/// <summary>
/// Interface for personality mapping service
/// </summary>
public interface IPersonalityMappingService
{
    /// <summary>
    /// Gets personality tags from two MBTI types
    /// </summary>
    List<string> GetPersonalityTags(string mbti1, string mbti2);
    
    /// <summary>
    /// Gets compatibility score between two MBTI types
    /// </summary>
    int GetCompatibilityScore(string mbti1, string mbti2);
}
