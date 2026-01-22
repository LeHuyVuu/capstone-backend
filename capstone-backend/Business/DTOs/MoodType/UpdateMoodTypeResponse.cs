namespace capstone_backend.Business.DTOs.Emotion;

/// <summary>
/// Response sau khi cập nhật mood type thành công
/// </summary>
public class UpdateMoodTypeResponse
{
    /// <summary>
    /// ID của mood type đã cập nhật
    /// </summary>
    public int MoodTypeId { get; set; }

    /// <summary>
    /// Tên mood type
    /// </summary>
    public string MoodTypeName { get; set; } = null!;

    /// <summary>
    /// Icon URL
    /// </summary>
    public string? IconUrl { get; set; }

    /// <summary>
    /// Thời gian cập nhật
    /// </summary>
    public DateTime UpdatedAt { get; set; }
}
