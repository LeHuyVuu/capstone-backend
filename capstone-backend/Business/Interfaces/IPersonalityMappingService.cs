namespace capstone_backend.Business.Interfaces;

/// <summary>
/// Interface for personality mapping service
/// Maps MBTI types to personality tags for venue recommendations
/// </summary>
public interface IPersonalityMappingService
{
    /// <summary>
    /// Maps two MBTI types to personality tags for couple recommendations
    /// Returns up to 5 personality tags based on MBTI characteristics
    /// </summary>
    List<string> GetPersonalityTags(string mbti1, string mbti2);
}
