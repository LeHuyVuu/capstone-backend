using capstone_backend.Business.Interfaces;
using capstone_backend.Data.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace capstone_backend.Business.Services;

/// <summary>
/// Service for mapping user moods to couple mood types based on predefined rules
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
    /// Implements the 12 couple mood mapping rules based on business requirements
    /// </summary>
    public async Task<string?> GetCoupleMoodTypeAsync(int mood1Id, int mood2Id)
    {
        // Load mood names from database using repository
        var mood1 = await _unitOfWork.MoodTypes.GetByIdAsync(mood1Id);
        var mood2 = await _unitOfWork.MoodTypes.GetByIdAsync(mood2Id);

        if (mood1 == null || mood2 == null)
            return null;

        return GetCoupleMoodType(mood1.Name, mood2.Name);
    }

    /// <summary>
    /// Determines couple mood type based on two mood names
    /// Implements 12 couple mood combination rules
    /// </summary>
    private string GetCoupleMoodType(string mood1, string mood2)
    {
        // Normalize moods to handle case-insensitivity
        mood1 = mood1?.ToLower() ?? "";
        mood2 = mood2?.ToLower() ?? "";

        // Normalize order for easier matching (alphabetical)
        if (string.Compare(mood1, mood2) > 0)
        {
            (mood1, mood2) = (mood2, mood1);
        }

        // Rule 1: Shared Happiness – Vui chung
        // Khi ít nhất 1 người HAPPY và người kia không tiêu cực mạnh (CALM, SURPRISED, CONFUSED)
        if ((mood1 == "vui" && (mood2 == "vui" || mood2 == "yên tĩnh" || mood2 == "bình thường" || mood2 == "hứng thú")) ||
            (mood1 == "yên tĩnh" && mood2 == "vui") ||
            (mood1 == "bình thường" && mood2 == "vui") ||
            (mood1 == "hứng thú" && mood2 == "vui"))
        {
            return "Vui chung";
        }

        // Rule 2: Mutual Calm – Cả hai yên tĩnh
        // Khi cả hai CALM hoặc một CALM + người kia trung tính (CONFUSED)
        if ((mood1 == "yên tĩnh" && mood2 == "yên tĩnh") ||
            (mood1 == "yên tĩnh" && mood2 == "bình thường") ||
            (mood1 == "bình thường" && mood2 == "yên tĩnh") ||
            (mood1 == "bình thường" && mood2 == "bình thường"))
        {
            return "Cả hai yên tĩnh";
        }

        // Rule 3: Comfort-Seeking – Cần được an ủi
        // Khi 1 người SAD và người kia không ANGRY/DISGUSTED
        if ((mood1 == "buồn" && mood2 != "tức giận" && mood2 != "ghê tởm") ||
            (mood2 == "buồn" && mood1 != "tức giận" && mood1 != "ghê tởm"))
        {
            return "Cần được an ủi";
        }

        // Rule 4: Stress & Tension – Căng thẳng hai chiều
        // Khi một hoặc cả hai: ANGRY, FEAR, DISGUSTED
        if (mood1 == "tức giận" || mood2 == "tức giận" || 
            mood1 == "sợ hãi" || mood2 == "sợ hãi" ||
            mood1 == "ghê tởm" || mood2 == "ghê tởm")
        {
            return "Căng thẳng hai chiều";
        }

        // Rule 5: Emotional Imbalance – Lệch pha cảm xúc
        // Khi 1 người HAPPY vs người kia SAD / ANGRY / FEAR / DISGUSTED
        if ((mood1 == "vui" && (mood2 == "buồn" || mood2 == "tức giận" || mood2 == "sợ hãi" || mood2 == "ghê tởm")) ||
            (mood2 == "vui" && (mood1 == "buồn" || mood1 == "tức giận" || mood1 == "sợ hãi" || mood1 == "ghê tởm")))
        {
            return "Lệch pha cảm xúc";
        }

        // Rule 6: Exploration Mood – Hứng thú khám phá
        // Khi bất kỳ người nào SURPRISED và người còn lại HAPPY/CALM/CONFUSED
        if ((mood1 == "hứng thú" && (mood2 == "vui" || mood2 == "yên tĩnh" || mood2 == "bình thường")) ||
            (mood2 == "hứng thú" && (mood1 == "vui" || mood1 == "yên tĩnh" || mood1 == "bình thường")))
        {
            return "Hứng thú khám phá";
        }

        // Rule 7: Playful but Sensitive – Vui nhưng dễ tổn thương
        // HAPPY + SAD, HAPPY + CONFUSED, HAPPY + FEAR
        if ((mood1 == "vui" && (mood2 == "buồn" || mood2 == "bình thường" || mood2 == "sợ hãi")) ||
            (mood2 == "vui" && (mood1 == "buồn" || mood1 == "bình thường" || mood1 == "sợ hãi")))
        {
            return "Vui nhưng dễ tổn thương";
        }

        // Rule 8: Reassurance Needed – Cần được trấn an
        // Khi 1 người FEAR/CONFUSED và người kia CALM/SURPRISED
        if ((mood1 == "sợ hãi" && (mood2 == "yên tĩnh" || mood2 == "hứng thú")) ||
            (mood2 == "sợ hãi" && (mood1 == "yên tĩnh" || mood1 == "hứng thú")) ||
            (mood1 == "bình thường" && (mood2 == "yên tĩnh" || mood2 == "hứng thú")) ||
            (mood2 == "bình thường" && (mood1 == "yên tĩnh" || mood1 == "hứng thú")))
        {
            return "Cần được trấn an";
        }

        // Rule 9: Low-Intimacy Boundary – Giảm thân mật
        // Khi bất kỳ người nào DISGUSTED
        if (mood1 == "ghê tởm" || mood2 == "ghê tởm")
        {
            return "Giảm thân mật";
        }

        // Rule 10: Resolution Mode – Cần hòa giải
        // ANGRY + SAD, ANGRY + CONFUSED, SAD + DISGUSTED
        if ((mood1 == "tức giận" && mood2 == "buồn") ||
            (mood1 == "tức giận" && mood2 == "bình thường") ||
            (mood1 == "buồn" && mood2 == "ghê tởm"))
        {
            return "Cần hòa giải";
        }

        // Rule 11: High-Energy Divergence – Năng lượng không đồng đều
        // HAPPY + ANGRY, SURPRISED + ANGRY
        if ((mood1 == "vui" && mood2 == "tức giận") ||
            (mood1 == "hứng thú" && mood2 == "tức giận"))
        {
            return "Năng lượng không đồng đều";
        }

        // Rule 12: Neutral / Mixed Mood – Trung tính
        // CONFUSED + CALM, CONFUSED + SURPRISED
        if ((mood1 == "bình thường" && (mood2 == "yên tĩnh" || mood2 == "hứng thú")))
        {
            return "Trung tính";
        }

        // Default fallback
        return "Trung tính";
    }

    public async Task<int?> GetCoupleMoodLocationTagIdAsync(int mood1Id, int mood2Id)
    {
        var coupleMoodType = await GetCoupleMoodTypeAsync(mood1Id, mood2Id);
        if (string.IsNullOrEmpty(coupleMoodType))
            return null;

        // Find the location tag that matches this couple mood type
        var matchingTag = await _unitOfWork.Context.Set<Data.Entities.LocationTag>()
            .Include(lt => lt.CoupleMoodType)
            .FirstOrDefaultAsync(lt => 
                lt.CoupleMoodType != null && 
                lt.CoupleMoodType.Name.Equals(coupleMoodType, StringComparison.OrdinalIgnoreCase));

        return matchingTag?.Id;
    }

}

/// <summary>
/// Interface for mood mapping service
/// </summary>
public interface IMoodMappingService
{
    /// <summary>
    /// Determines couple mood type from two mood IDs
    /// </summary>
    Task<string?> GetCoupleMoodTypeAsync(int mood1Id, int mood2Id);
    
    /// <summary>
    /// Gets the LocationTag ID for the couple mood type
    /// </summary>
    Task<int?> GetCoupleMoodLocationTagIdAsync(int mood1Id, int mood2Id);
}
