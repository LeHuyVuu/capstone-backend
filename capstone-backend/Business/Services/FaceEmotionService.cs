using Amazon.Rekognition;
using Amazon.Rekognition.Model;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats.Jpeg;
using AwsImage = Amazon.Rekognition.Model.Image;
using SixImage = SixLabors.ImageSharp.Image;

namespace capstone_backend.Business.Services;

/// <summary>
/// Service để phân tích cảm xúc khuôn mặt sử dụng AWS Rekognition - Đã tối ưu hiệu năng
/// </summary>
public class FaceEmotionService
{
    private readonly IAmazonRekognition _rekognitionClient;
    private readonly ILogger<FaceEmotionService> _logger;

    // Cấu hình tối ưu
    private const int MaxImageWidth = 1200;  // Giảm resolution để nhanh hơn
    private const int MaxImageHeight = 1200;
    private const int JpegQuality = 85;      // Nén ảnh để giảm dung lượng

    public FaceEmotionService(IAmazonRekognition rekognitionClient, ILogger<FaceEmotionService> logger)
    {
        _rekognitionClient = rekognitionClient;
        _logger = logger;
    }

    /// <summary>
    /// Tối ưu ảnh: resize và nén để tăng tốc độ phân tích
    /// </summary>
    private async Task<byte[]> OptimizeImageAsync(byte[] imageBytes)
    {
        using var inputStream = new MemoryStream(imageBytes);
        using var image = await SixImage.LoadAsync(inputStream);
        var originalSize = imageBytes.Length;

        // Resize nếu ảnh quá lớn
        if (image.Width > MaxImageWidth || image.Height > MaxImageHeight)
        {
            image.Mutate(x => x.Resize(new ResizeOptions
            {
                Size = new SixLabors.ImageSharp.Size(MaxImageWidth, MaxImageHeight),
                Mode = ResizeMode.Max  // Giữ tỷ lệ, không bóp méo
            }));
        }

        // Nén ảnh thành JPEG chất lượng 85%
        using var outputStream = new MemoryStream();
        await image.SaveAsJpegAsync(outputStream, new JpegEncoder { Quality = JpegQuality });
        var optimizedBytes = outputStream.ToArray();

        var savedPercent = (1 - (double)optimizedBytes.Length / originalSize) * 100;
        _logger.LogInformation($"🚀 Tối ưu ảnh: {originalSize / 1024}KB → {optimizedBytes.Length / 1024}KB (tiết kiệm {savedPercent:F1}%)");

        return optimizedBytes;
    }

    /// <summary>
    /// Phát hiện và phân tích cảm xúc khuôn mặt từ ảnh - ĐÃ TỐI ƯU HIỆU NĂNG
    /// </summary>
    /// <param name="imageBytes">Dữ liệu ảnh dưới dạng byte array</param>
    /// <returns>Danh sách các khuôn mặt được phát hiện với thông tin cảm xúc</returns>
    public async Task<List<FaceDetail>> DetectFacesAsync(byte[] imageBytes)
    {
        try
        {
            // Bước 1: Tối ưu ảnh trước khi gửi AWS (TĂNG TỐC ĐỘ)
            var optimizedImage = await OptimizeImageAsync(imageBytes);

            // Bước 2: Chỉ lấy attributes cần thiết thay vì ALL (GIẢM THỜI GIAN XỬ LÝ)
            var request = new DetectFacesRequest
            {
                Image = new AwsImage
                {
                    Bytes = new MemoryStream(optimizedImage)
                },
                Attributes = new List<string> { "DEFAULT", "ALL" } // Chỉ lấy cảm xúc, tuổi, giới tính
            };

            var startTime = DateTime.UtcNow;
            var response = await _rekognitionClient.DetectFacesAsync(request);
            var duration = (DateTime.UtcNow - startTime).TotalMilliseconds;

            _logger.LogInformation($"✅ Phát hiện {response.FaceDetails.Count} khuôn mặt trong {duration}ms");

            return response.FaceDetails;
        }
        catch (AccessDeniedException ex)
        {
            _logger.LogError(ex, "❌ AWS IAM User không có quyền sử dụng Rekognition");
            throw new InvalidOperationException(
                "AWS credentials không có quyền sử dụng Rekognition. " +
                "Vui lòng thêm policy 'AmazonRekognitionFullAccess' cho IAM user.", ex);
        }
        catch (InvalidImageFormatException ex)
        {
            _logger.LogError(ex, "❌ Định dạng ảnh không hợp lệ");
            throw new InvalidOperationException("Định dạng ảnh không được hỗ trợ bởi AWS Rekognition.", ex);
        }
        catch (ImageTooLargeException ex)
        {
            _logger.LogError(ex, "❌ Ảnh quá lớn");
            throw new InvalidOperationException("Kích thước ảnh vượt quá giới hạn của AWS Rekognition (15MB).", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Lỗi không xác định khi phân tích khuôn mặt");
            throw;
        }
    }

    /// <summary>
    /// Lấy cảm xúc chủ đạo của khuôn mặt (cảm xúc có độ tin cậy cao nhất)
    /// </summary>
    /// <param name="face">Thông tin khuôn mặt</param>
    /// <returns>Tên cảm xúc chủ đạo</returns>
    public string GetDominantEmotion(FaceDetail face)
    {
        var dominantEmotion = face.Emotions
            .OrderByDescending(e => e.Confidence)
            .FirstOrDefault();

        return dominantEmotion?.Type.Value ?? "Unknown";
    }

    /// <summary>
    /// Lấy danh sách tất cả cảm xúc của khuôn mặt với độ tin cậy
    /// </summary>
    /// <param name="face">Thông tin khuôn mặt</param>
    /// <returns>Dictionary với key là tên cảm xúc, value là độ tin cậy (%)</returns>
    public Dictionary<string, decimal> GetAllEmotions(FaceDetail face)
    {
        return face.Emotions
            .Where(e => e.Confidence.HasValue && e.Type?.Value != null)
            .OrderByDescending(e => e.Confidence!.Value)
            .ToDictionary(
                e => e.Type!.Value,
                e => Math.Round((decimal)e.Confidence!.Value, 2)
            );
    }
    
    
    
    public string GetEmotionSentence(string emotion)
    {
        return emotion switch
        {
            "HAPPY" => "Chúng mình thấy bạn đang zuiii lắm đó, phải hông nè 😊",
            "SAD" => "Hình như bạn đang hơi buồn một chút, tụi mình ở đây nè 💙",
            "ANGRY" => "Có vẻ bạn đang hơi căng thẳng đó, hít thở sâu nha 😤",
            "SURPRISED" => "Woww, trông bạn có vẻ rất bất ngờ luôn 😲",
            "CONFUSED" => "Bạn đang hơi bối rối đúng hông ta 🤔",
            "DISGUSTED" => "Có gì đó làm bạn không thoải mái thì phải 😖",
            "CALM" => "Bạn trông rất bình tĩnh và thư giãn luôn 😌",
            "FEAR" => "Có vẻ bạn đang hơi lo lắng đó, đừng sợ nha 🫂",
            _ => "Tụi mình chưa đoán rõ cảm xúc của bạn, nhưng bạn vẫn dễ thương lắm ✨"
        };
    }

}
