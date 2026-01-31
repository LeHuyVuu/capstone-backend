namespace capstone_backend.Business.Interfaces;

/// <summary>
/// Interface for mood mapping service
/// Maps individual moods to couple mood types for venue recommendations
/// </summary>
public interface IMoodMappingService
{
    /// <summary>
    /// Determines couple mood type from two mood IDs
    /// </summary>
    Task<string?> GetCoupleMoodTypeAsync(int mood1Id, int mood2Id);
    

}
