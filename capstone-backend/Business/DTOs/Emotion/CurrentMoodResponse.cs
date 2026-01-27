namespace capstone_backend.Business.DTOs.Emotion;

public class CurrentMoodResponse
{
    /// <summary>
    /// ID của thành viên
    /// </summary>
    public int MemberId { get; set; }

    /// <summary>
    /// Tâm trạng hiện tại của thành viên
    /// </summary>
    public string? CurrentMood { get; set; }

    /// <summary>
    /// ID của tâm trạng hiện tại
    /// </summary>
    public int? CurrentMoodId { get; set; }

    /// <summary>
    /// Thời gian cập nhật tâm trạng gần nhất
    /// </summary>
    public DateTime? MoodUpdatedAt { get; set; }

    /// <summary>
    /// ID của partner trong cặp đôi (nếu có)
    /// </summary>
    public int? PartnerMemberId { get; set; }

    /// <summary>
    /// Tâm trạng của partner (nếu có)
    /// </summary>
    public string? PartnerMood { get; set; }

    /// <summary>
    /// ID của tâm trạng của partner
    /// </summary>
    public int? PartnerMoodId { get; set; }

    /// <summary>
    /// Thời gian cập nhật tâm trạng của partner
    /// </summary>
    public DateTime? PartnerMoodUpdatedAt { get; set; }

    /// <summary>
    /// Tâm trạng cặp đôi (nếu cả 2 đều có tâm trạng)
    /// </summary>
    public string? CoupleMood { get; set; }

    /// <summary>
    /// Mô tả chi tiết về tâm trạng cặp đôi và gợi ý hoạt động
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Liệu có phải là cặp đôi và có đủ dữ liệu không
    /// </summary>
    public bool IsCouple { get; set; }

    /// <summary>
    /// Liệu có tâm trạng cặp đôi không
    /// </summary>
    public bool HasCoupleMood { get; set; }
}
