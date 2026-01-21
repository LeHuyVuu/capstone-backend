namespace capstone_backend.Business.DTOs.Emotion;

/// <summary>
/// Response chứa thông tin cảm xúc của khuôn mặt
/// </summary>
public class FaceEmotionResponse
{
    /// <summary>
    /// Cảm xúc chủ đạo (có độ tin cậy cao nhất)
    /// VD: HAPPY, SAD, ANGRY, CONFUSED, DISGUSTED, SURPRISED, CALM, FEAR
    /// </summary>
    public string DominantEmotion { get; set; } = string.Empty;

    /// <summary>
    /// Danh sách tất cả cảm xúc với độ tin cậy (%)
    /// </summary>
    public Dictionary<string, decimal> AllEmotions { get; set; } = new();

    /// <summary>
    /// Độ tuổi ước tính (khoảng min-max)
    /// VD: "25-35"
    /// </summary>
    public string AgeRange { get; set; } = string.Empty;

    /// <summary>
    /// Giới tính dự đoán
    /// VD: "Male", "Female"
    /// </summary>
    public string Gender { get; set; } = string.Empty;

    /// <summary>
    /// Độ tin cậy giới tính (%)
    /// </summary>
    public decimal GenderConfidence { get; set; }

    /// <summary>
    /// Có đeo kính hay không
    /// </summary>
    public bool HasSunglasses { get; set; }

    /// <summary>
    /// Có cười hay không
    /// </summary>
    public bool IsSmiling { get; set; }

    /// <summary>
    /// Độ tin cậy của nụ cười (%)
    /// </summary>
    public decimal SmileConfidence { get; set; }
    
    public string EmotionSentence { get; set; }   // 👈 thêm dòng này

}

/// <summary>
/// Request để phân tích cảm xúc
/// </summary>
public class AnalyzeEmotionRequest
{
    /// <summary>
    /// File ảnh cần phân tích
    /// </summary>
    public IFormFile Image { get; set; } = null!;
}
