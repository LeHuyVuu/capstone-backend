using capstone_backend.Business.Interfaces;

namespace capstone_backend.Business.Services;

/// <summary>
/// Service for mapping MBTI personality types to couple personality tags
/// Uses Keirsey Temperament Sorter (NF, SP, SJ, NT) logic
/// </summary>
public class PersonalityMappingService : IPersonalityMappingService
{
    // Cache for personality tags to avoid recalculation
    private static readonly Dictionary<string, List<string>> _personalityCache = new();
    private static readonly object _cacheLock = new();

    /// <summary>
    /// Maps MBTI types to a single personality tag based on Keirsey Rules
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

        // Determine the single most appropriate tag
        string selectedTag = DetermineTag(mbti1, mbti2);
        var result = new List<string> { selectedTag };
        
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
    /// Core logic to determine the tag based on Single or Couple rules
    /// </summary>
    private string DetermineTag(string mbti1, string mbti2)
    {
        // 1. CASE SINGLE: Only mbti1 is provided
        if (!string.IsNullOrEmpty(mbti1) && string.IsNullOrEmpty(mbti2))
        {
             if (mbti1.Length < 4) return "HÒA GIẢI"; // Fallback
             return MapGroupToTag(GetKeirseyGroup(mbti1.ToUpper()));
        }

        // 2. CASE COUPLE: Both are provided
        if (!string.IsNullOrEmpty(mbti1) && !string.IsNullOrEmpty(mbti2))
        {
            if (mbti1.Length < 4 || mbti2.Length < 4) return "HÒA GIẢI"; // Fallback

            mbti1 = mbti1.ToUpper();
            mbti2 = mbti2.ToUpper();

            // RULE 1 (Priority): High Energy Couple (Both are Extroverts) -> VUI VẺ
            if (mbti1[0] == 'E' && mbti2[0] == 'E') 
                return "VUI VẺ";

            // RULE 2: Check Keirsey Temperament Groups
            string group1 = GetKeirseyGroup(mbti1);
            string group2 = GetKeirseyGroup(mbti2);

            // If same group, map to that group's characteristic
            if (group1 == group2)
            {
                return MapGroupToTag(group1);
            }

            // RULE 3: Mixed groups or Rational Logic (NT + NT) -> HÒA GIẢI
            // NT group (Rationals) prefer Logic/Debate -> HÒA GIẢI fits best among options
            return "HÒA GIẢI";
        }

        return "HÒA GIẢI"; // Default safe fallback
    }

    /// <summary>
    /// Classifies MBTI into 4 Keirsey Temperaments: NF, NT, SP, SJ
    /// </summary>
    private string GetKeirseyGroup(string mbti)
    {
        // Safety check
        if (string.IsNullOrEmpty(mbti) || mbti.Length < 4) return "UNKNOWN";

        bool isN = mbti[1] == 'N'; // Intuition
        bool isS = mbti[1] == 'S'; // Sensing

        // NF: Idealists (Mơ mộng) - Intuition + Feeling
        if (isN && mbti[2] == 'F') return "NF"; 
        
        // NT: Rationals (Lý trí) - Intuition + Thinking
        if (isN && mbti[2] == 'T') return "NT"; 
        
        // SJ: Guardians (Ổn định) - Sensing + Judging
        if (isS && mbti[3] == 'J') return "SJ"; 
        
        // SP: Artisans (Trải nghiệm) - Sensing + Perceiving
        if (isS && mbti[3] == 'P') return "SP"; 

        return "UNKNOWN";
    }

    /// <summary>
    /// Maps Keirsey Group directly to database Tags
    /// </summary>
    private string MapGroupToTag(string group)
    {
        return group switch
        {
            "NF" => "LÃNG MẠN", // Idealists -> Romantic
            "SP" => "PHIÊU LƯU", // Artisans -> Adventurous
            "SJ" => "THƯ THÁI", // Guardians -> Relaxed/Stable
            "NT" => "HÒA GIẢI", // Rationals -> Harmony/Logic resolution
            _ => "HÒA GIẢI"
        };
    }
}

