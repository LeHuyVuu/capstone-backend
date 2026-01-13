using capstone_backend.Api.Models;
using capstone_backend.Business.DTOs.Emotion;
using capstone_backend.Business.Services;
using Microsoft.AspNetCore.Mvc;

namespace capstone_backend.Api.Controllers;

/// <summary>
/// API để phân tích cảm xúc khuôn mặt sử dụng AWS Rekognition
/// </summary>
[ApiController]
[Route("api/v2/[controller]")]
public class EmotionController : BaseController
{
    private readonly FaceEmotionService _emotionService;
    private readonly ILogger<EmotionController> _logger;

    public EmotionController(FaceEmotionService emotionService, ILogger<EmotionController> logger)
    {
        _emotionService = emotionService;
        _logger = logger;
    }

    /// <summary>
    /// Phân tích cảm xúc khuôn mặt từ ảnh
    /// </summary>
    /// <param name="image">File ảnh cần phân tích (JPG, PNG)</param>
    /// <returns>Danh sách cảm xúc của tất cả khuôn mặt trong ảnh</returns>
    /// <response code="200">Phân tích thành công</response>
    /// <response code="400">File ảnh không hợp lệ hoặc không có khuôn mặt</response>
    /// <response code="500">Lỗi khi gọi AWS Rekognition</response>
    [HttpPost("analyze")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ApiResponse<List<FaceEmotionResponse>>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    public async Task<IActionResult> AnalyzeEmotion(IFormFile image)
    {
        var startTime = DateTime.UtcNow;

        // Kiểm tra file có tồn tại không
        if (image == null || image.Length == 0)
        {
            return BadRequest(ApiResponse<object>.Error("Vui lòng upload file ảnh", 400));
        }

        // Kiểm tra định dạng file (chỉ chấp nhận ảnh)
        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
        var fileExtension = Path.GetExtension(image.FileName).ToLowerInvariant();
        
        if (!allowedExtensions.Contains(fileExtension))
        {
            return BadRequest(ApiResponse<object>.Error(
                "Chỉ chấp nhận file ảnh định dạng JPG hoặc PNG", 400));
        }

        // Tăng giới hạn lên 10MB (sẽ tự động resize)
        const int maxFileSize = 10 * 1024 * 1024; // 10MB
        if (image.Length > maxFileSize)
        {
            return BadRequest(ApiResponse<object>.Error(
                "Kích thước file không được vượt quá 10MB", 400));
        }

        try
        {
            // Đọc file ảnh trực tiếp vào byte array (NHANH HƠN)
            using var memoryStream = new MemoryStream((int)image.Length);
            await image.CopyToAsync(memoryStream);
            var imageBytes = memoryStream.ToArray();

            _logger.LogInformation($"📸 Upload: {image.FileName} ({image.Length / 1024}KB)");

            // Gọi AWS Rekognition để phân tích
            var faces = await _emotionService.DetectFacesAsync(imageBytes);

            // Kiểm tra có phát hiện khuôn mặt không
            if (faces.Count == 0)
            {
                return BadRequest(ApiResponse<object>.Error(
                    "Không phát hiện khuôn mặt nào trong ảnh", 400));
            }

            Console.Write("Data trả về nè:" + faces);
            // Chuyển đổi kết quả sang DTO dễ hiểu
            // Chuyển đổi kết quả sang DTO dễ hiểu
            var results = faces.Select(face => new FaceEmotionResponse
            {
                DominantEmotion = _emotionService.GetDominantEmotion(face),
                AllEmotions = _emotionService.GetAllEmotions(face),
                AgeRange = $"{face.AgeRange.Low}-{face.AgeRange.High}",
                Gender = face.Gender?.Value ?? "Unknown",
                GenderConfidence = face.Gender?.Confidence != null ? Math.Round((decimal)face.Gender.Confidence, 2) : 0,
                HasSunglasses = face.Sunglasses?.Value ?? false,
                IsSmiling = face.Smile?.Value ?? false,
                SmileConfidence = face.Smile?.Confidence != null ? Math.Round((decimal)face.Smile.Confidence, 2) : 0
            }).ToList();

            var totalTime = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogInformation($"⚡ Hoàn thành phân tích {results.Count} khuôn mặt trong {totalTime}ms");

            return Ok(ApiResponse<List<FaceEmotionResponse>>.Success(
                results,
                $"Phát hiện {results.Count} khuôn mặt trong {totalTime:F0}ms"));
        }
        catch (InvalidOperationException ex)
        {
            // Lỗi từ service (AWS permissions, invalid format, v.v.)
            _logger.LogError(ex, "Lỗi từ AWS Rekognition");
            return StatusCode(403, ApiResponse<object>.Error(ex.Message, 403));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi không xác định khi phân tích cảm xúc");
            return StatusCode(500, ApiResponse<object>.Error(
                "Có lỗi xảy ra khi phân tích ảnh. Vui lòng thử lại.", 500));
        }
    }
}
