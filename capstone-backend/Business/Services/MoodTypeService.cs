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
        var moodTypes = await _unitOfWork.Context.Set<MoodType>()
            .Where(m => m.IsDeleted != true && m.IsActive == true)
            .OrderBy(m => m.Name)
            .ToListAsync(cancellationToken);

        return moodTypes.Select(MapToResponse).ToList();
    }

    public async Task<MoodTypeResponse?> GetMoodTypeByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var moodType = await _unitOfWork.Context.Set<MoodType>()
            .FirstOrDefaultAsync(m => m.Id == id && m.IsDeleted != true, cancellationToken);

        return moodType == null ? null : MapToResponse(moodType);
    }

    private MoodTypeResponse MapToResponse(MoodType moodType)
    {
        return new MoodTypeResponse
        {
            Id = moodType.Id,
            Name = moodType.Name,
            IconUrl = moodType.IconUrl,
            IsActive = moodType.IsActive,
            CreatedAt = moodType.CreatedAt
        };
    }
}
