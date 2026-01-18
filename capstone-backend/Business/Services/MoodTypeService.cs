using capstone_backend.Business.DTOs.MoodType;
using capstone_backend.Business.Interfaces;
using capstone_backend.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace capstone_backend.Business.Services;

public class MoodTypeService : IMoodTypeService
{
    private readonly IUnitOfWork _unitOfWork;

    public MoodTypeService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<List<MoodTypeResponse>> GetAllMoodTypesAsync(CancellationToken cancellationToken = default)
    {
        var moodTypes = await _unitOfWork.Context.Set<mood_type>()
            .Where(m => m.is_deleted != true && m.is_active == true)
            .OrderBy(m => m.name)
            .ToListAsync(cancellationToken);

        return moodTypes.Select(MapToResponse).ToList();
    }

    public async Task<MoodTypeResponse?> GetMoodTypeByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var moodType = await _unitOfWork.Context.Set<mood_type>()
            .FirstOrDefaultAsync(m => m.id == id && m.is_deleted != true, cancellationToken);

        return moodType == null ? null : MapToResponse(moodType);
    }

    private MoodTypeResponse MapToResponse(mood_type moodType)
    {
        return new MoodTypeResponse
        {
            Id = moodType.id,
            Name = moodType.name,
            IconUrl = moodType.icon_url,
            IsActive = moodType.is_active,
            CreatedAt = moodType.created_at
        };
    }
}
