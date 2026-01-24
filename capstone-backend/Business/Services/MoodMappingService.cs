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
    /// Implements the 12 couple mood mapping rules
    /// </summary>
    public async Task<string?> GetCoupleMoodTypeAsync(int mood1Id, int mood2Id)
    {
        // Load mood names from database using Context
        var mood1 = await _unitOfWork.Context.Set<Data.Entities.MoodType>()
            .FirstOrDefaultAsync(m => m.Id == mood1Id);
        var mood2 = await _unitOfWork.Context.Set<Data.Entities.MoodType>()
            .FirstOrDefaultAsync(m => m.Id == mood2Id);

        if (mood1 == null || mood2 == null)
            return null;

        var moodPair = NormalizeMoodPair(mood1.Name, mood2.Name);
        
        return moodPair switch
        {
            // Rule 1: Vui + Vui → Hạnh phúc
            ("Vui", "Vui") => "Hạnh phúc",
            
            // Rule 2: Vui + Buồn → Động viên
            ("Vui", "Buồn") or ("Buồn", "Vui") => "Động viên",
            
            // Rule 3: Vui + Bình thường → Thoải mái
            ("Vui", "Bình thường") or ("Bình thường", "Vui") => "Thoải mái",
            
            // Rule 4: Vui + Hào hứng → Phấn khích
            ("Vui", "Hào hứng") or ("Hào hứng", "Vui") => "Phấn khích",
            
            // Rule 5: Buồn + Buồn → Chia sẻ
            ("Buồn", "Buồn") => "Chia sẻ",
            
            // Rule 6: Buồn + Bình thường → An ủi
            ("Buồn", "Bình thường") or ("Bình thường", "Buồn") => "An ủi",
            
            // Rule 7: Buồn + Hào hứng → Động viên
            ("Buồn", "Hào hứng") or ("Hào hứng", "Buồn") => "Động viên",
            
            // Rule 8: Bình thường + Bình thường → Thư giãn
            ("Bình thường", "Bình thường") => "Thư giãn",
            
            // Rule 9: Bình thường + Hào hứng → Khám phá
            ("Bình thường", "Hào hứng") or ("Hào hứng", "Bình thường") => "Khám phá",
            
            // Rule 10: Hào hứng + Hào hứng → Mạo hiểm
            ("Hào hứng", "Hào hứng") => "Mạo hiểm",
            
            // Rule 11: Stressed + Stressed → Thư giãn
            ("Stressed", "Stressed") => "Thư giãn",
            
            // Rule 12: Stressed + any other → An ủi
            ("Stressed", _) or (_, "Stressed") => "An ủi",
            
            // Default: Thoải mái
            _ => "Thoải mái"
        };
    }

    /// <summary>
    /// Gets couple mood type from IDs and returns the corresponding LocationTag ID
    /// </summary>
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

    /// <summary>
    /// Normalizes mood pair order for consistent matching
    /// </summary>
    private (string, string) NormalizeMoodPair(string mood1, string mood2)
    {
        // Always put "Stressed" first for easier matching
        if (mood2.Equals("Stressed", StringComparison.OrdinalIgnoreCase) && 
            !mood1.Equals("Stressed", StringComparison.OrdinalIgnoreCase))
        {
            return (mood2, mood1);
        }

        return (mood1, mood2);
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
