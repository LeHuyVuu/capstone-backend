using capstone_backend.Business.Interfaces;
using capstone_backend.Business.Recommendation;
using capstone_backend.Data.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace capstone_backend.Business.Services;

/// <summary>
/// Service for mapping user moods to couple mood types based on predefined business rules
/// 
/// Handles mapping from 8 individual moods (AWS Rekognition):
/// HAPPY, DISGUSTED, SURPRISED, CALM, FEAR, CONFUSED, ANGRY, SAD
/// 
/// To 12 couple mood types:
/// 1. Shared Happiness (Vui chung)
/// 2. Mutual Calm (Yên tĩnh)
/// 3. Comfort-Seeking (Cần an ủi)
/// 4. Stress and Tension (Căng thẳng)
/// 5. Emotional Imbalance (Lệch pha cảm xúc)
/// 6. Exploration Mood (Hứng thú khám phá)
/// 7. Playful but Sensitive (Vui nhưng dễ tổn thương)
/// 8. Reassurance Needed (Cần được trấn an)
/// 9. Low-Intimacy Boundary (Giảm thân mật)
/// 10. Resolution Mode (Cần hòa giải)
/// 11. High-Energy Divergence (Năng lượng không đồng đều)
/// 12. Neutral / Mixed Mood (Trung tính)
/// </summary>
public class MoodMappingService : IMoodMappingService
{
    private readonly IUnitOfWork _unitOfWork;

    public MoodMappingService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    /// <summary>
    /// Determines the couple mood type based on two individual mood IDs
    /// Implements 12 couple mood mapping rules via CoupleMoodMapper
    /// Implements the 12 couple mood mapping rules based on business requirements
    /// </summary>
    public async Task<string?> GetCoupleMoodTypeAsync(int mood1Id, int mood2Id)
    {
        // Load mood names from database using repository
        var mood1 = await _unitOfWork.MoodTypes.GetByIdAsync(mood1Id);
        var mood2 = await _unitOfWork.MoodTypes.GetByIdAsync(mood2Id);

        if (mood1 == null || mood2 == null)
            return null;

        // Use new mapper based on 8 moods from AWS (HAPPY, DISGUSTED, SURPRISED, CALM, FEAR, CONFUSED, ANGRY, SAD)
        return CoupleMoodMapper.MapToCoupleeMood(mood1.Name, mood2.Name);
    }    


    // public async Task<int?> GetCoupleMoodLocationTagIdAsync(int mood1Id, int mood2Id)
    // {
    //     var coupleMoodType = await GetCoupleMoodTypeAsync(mood1Id, mood2Id);
    //     if (string.IsNullOrEmpty(coupleMoodType))
    //         return null;

    //     // Find the location tag that matches this couple mood type
    //     var matchingTag = await _unitOfWork.Context.Set<Data.Entities.LocationTag>()
    //         .Include(lt => lt.CoupleMoodType)
    //         .FirstOrDefaultAsync(lt => 
    //             lt.CoupleMoodType != null && 
    //             lt.CoupleMoodType.Name.Equals(coupleMoodType, StringComparison.OrdinalIgnoreCase));

    //     return matchingTag?.Id;
    // }
}

