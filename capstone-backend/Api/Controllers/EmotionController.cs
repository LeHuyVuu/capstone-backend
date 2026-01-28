using capstone_backend.Api.Models;
using capstone_backend.Business.DTOs.Emotion;
using capstone_backend.Business.Services;
using capstone_backend.Business.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace capstone_backend.Api.Controllers;

/// <summary>
/// API để phân tích cảm xúc khuôn mặt sử dụng AWS Rekognition
/// </summary>
[Route("api/[controller]")]
[ApiController]
// [Authorize] // TẠM THỜI BỎ ĐỂ TEST - SAU NÀY BẬT LẠI
public class EmotionController : BaseController
{
    private readonly FaceEmotionService _emotionService;
    private readonly ILogger<EmotionController> _logger;
    private readonly IUnitOfWork _unitOfWork;

    public EmotionController(FaceEmotionService emotionService, ILogger<EmotionController> logger, IUnitOfWork unitOfWork)
    {
        _emotionService = emotionService;
        _logger = logger;
        _unitOfWork = unitOfWork;
    }

    /// <summary>
    /// Phân tích cảm xúc khuôn mặt từ ảnh
    /// </summary>
    /// <param name="image">File ảnh cần phân tích (JPG, PNG)</param>
    /// <returns>Danh sách cảm xúc của tất cả khuôn mặt trong ảnh</returns>
    /// <response code="200">Phân tích thành công</response>
    /// <response code="400">File ảnh không hợp lệ hoặc không có khuôn mặt</response>
    /// <response code="401">Chưa xác thực hoặc token không hợp lệ</response>
    /// <response code="500">Lỗi khi gọi AWS Rekognition</response>
    /// <remarks>
   
    /// </remarks>
    [HttpPost("analyze")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ApiResponse<List<FaceEmotionResponse>>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    [ProducesResponseType(typeof(ApiResponse<object>), 401)]
    public async Task<IActionResult> AnalyzeEmotion(IFormFile image)
    {
        var startTime = DateTime.UtcNow;

        // ✅ LOG: Kiểm tra xem có token không
        var authHeader = Request.Headers["Authorization"].FirstOrDefault();
        var queryToken = Request.Query["access_token"].FirstOrDefault();
        var formToken = Request.Form.ContainsKey("token") ? Request.Form["token"].ToString() : null;
        
        _logger.LogInformation($"🔑 Authorization Header: {authHeader ?? "NULL"}");
        _logger.LogInformation($"🔑 Query Token: {queryToken ?? "NULL"}");
        _logger.LogInformation($"🔑 Form Token: {formToken ?? "NULL"}");
        _logger.LogInformation($"🔑 User.Identity.IsAuthenticated: {User.Identity?.IsAuthenticated}");
        _logger.LogInformation($"🔑 Current UserId: {GetCurrentUserId()}");

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
            var results = faces.Select(face =>
            {
                var dominantEmotion = _emotionService.GetDominantEmotion(face);       // HAPPY, SAD,...
                var dominantEmotionVi = FaceEmotionService.MapEmotionToVietnamese(dominantEmotion);

                return new FaceEmotionResponse
                {
                    DominantEmotion = dominantEmotionVi,                              // Vui, Buồn,...
                    EmotionSentence = FaceEmotionService.GetEmotionSentence(dominantEmotion),
                    AllEmotions = _emotionService.GetAllEmotions(face),
                    AgeRange = $"{face.AgeRange.Low}-{face.AgeRange.High}",
                    Gender = face.Gender?.Value ?? "Unknown",
                    GenderConfidence = face.Gender?.Confidence != null
                        ? Math.Round((decimal)face.Gender.Confidence, 2)
                        : 0,
                    HasSunglasses = face.Sunglasses?.Value ?? false,
                    IsSmiling = face.Smile?.Value ?? false,
                    SmileConfidence = face.Smile?.Confidence != null
                        ? Math.Round((decimal)face.Smile.Confidence, 2)
                        : 0
                };
            }).ToList();

            // Cập nhật MoodTypesId vào MemberProfile dựa trên emotion đầu tiên
            if (results.Count > 0)
            {
                var userId = GetCurrentUserId();
                if (userId.HasValue)
                {
                    try
                    {
                        var firstEmotion = _emotionService.GetDominantEmotion(faces[0]); // HAPPY, SAD, ...
                        
                        // Query MoodType dựa trên emotion name
                        var moodType = await _unitOfWork.Context.MoodTypes
                            .FirstOrDefaultAsync(m => m.Name.ToUpper() == firstEmotion.ToUpper() 
                                                    && m.IsDeleted != true 
                                                    && m.IsActive == true);

                        if (moodType != null)
                        {
                            // Lấy MemberProfile của user
                            var memberProfile = await _unitOfWork.MembersProfile.GetByUserIdAsync(userId.Value);
                            
                            if (memberProfile != null)
                            {
                                // Cập nhật MoodTypesId
                                memberProfile.MoodTypesId = moodType.Id;
                                memberProfile.UpdatedAt = DateTime.UtcNow;
                                
                                _unitOfWork.MembersProfile.Update(memberProfile);
                                await _unitOfWork.SaveChangesAsync();
                                
                                _logger.LogInformation($"✅ Đã cập nhật MoodTypesId={moodType.Id} ({moodType.Name}) cho UserId={userId.Value}");
                            }
                        }
                        else
                        {
                            _logger.LogWarning($"⚠️ Không tìm thấy MoodType với tên '{firstEmotion}'");
                        }
                    }
                    catch (Exception ex)
                    {
                        // Log lỗi nhưng vẫn trả về kết quả emotion
                        _logger.LogError(ex, "❌ Lỗi khi cập nhật MoodTypesId");
                    }
                }
            }



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
