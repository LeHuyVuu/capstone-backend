using System.ComponentModel.DataAnnotations;

namespace capstone_backend.Business.DTOs.Emotion;

/// <summary>
/// Request để cập nhật mood type vào member profile
/// </summary>
public class UpdateMoodTypeRequest
{
    /// <summary>
    /// ID của mood type được chọn
    /// </summary>
    [Required(ErrorMessage = "Mood type ID không được để trống")]
    [Range(1, int.MaxValue, ErrorMessage = "Mood type ID phải lớn hơn 0")]
    public int MoodTypeId { get; set; }
}
