using capstone_backend.Business.DTOs.MoodType;

namespace capstone_backend.Business.Interfaces;

public interface IMoodTypeService
{
    Task<List<MoodTypeResponse>> GetAllMoodTypesAsync(string? gender, CancellationToken cancellationToken = default);
    Task<MoodTypeResponse?> GetMoodTypeByIdAsync(int id, string? gender, CancellationToken cancellationToken = default);
}