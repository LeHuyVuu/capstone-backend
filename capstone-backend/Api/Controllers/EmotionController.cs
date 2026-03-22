using capstone_backend.Api.Models;
using capstone_backend.Business.DTOs.Emotion;
using capstone_backend.Business.Services;
using capstone_backend.Business.Interfaces;
using capstone_backend.Data.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace capstone_backend.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class EmotionController : BaseController
{
    private readonly FaceEmotionService _emotionService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMoodMappingService _moodMappingService;
    private readonly IChallengeService _challengeService;

    public EmotionController(FaceEmotionService emotionService, IUnitOfWork unitOfWork, IMoodMappingService moodMappingService, IChallengeService challengeService)
    {
        _emotionService = emotionService;
        _unitOfWork = unitOfWork;
        _moodMappingService = moodMappingService;
        _challengeService = challengeService;
    }

    [HttpPost("analyze")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> AnalyzeEmotion(IFormFile image)
    {
        if (image == null || image.Length == 0)
            return BadRequest(ApiResponse<object>.Error("Vui lòng upload file ảnh", 400));

        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
        var fileExtension = Path.GetExtension(image.FileName).ToLowerInvariant();
        
        if (!allowedExtensions.Contains(fileExtension))
            return BadRequest(ApiResponse<object>.Error("Chỉ chấp nhận file ảnh định dạng JPG hoặc PNG", 400));

        const int maxFileSize = 10 * 1024 * 1024;
        if (image.Length > maxFileSize)
            return BadRequest(ApiResponse<object>.Error("Kích thước file không được vượt quá 10MB", 400));

        try
        {
            using var memoryStream = new MemoryStream((int)image.Length);
            await image.CopyToAsync(memoryStream);
            var imageBytes = memoryStream.ToArray();

            var faces = await _emotionService.DetectFacesAsync(imageBytes);

            if (faces.Count == 0)
                return BadRequest(ApiResponse<object>.Error("Không phát hiện khuôn mặt nào trong ảnh", 400));

            var results = faces.Select(face =>
            {
                var dominantEmotion = _emotionService.GetDominantEmotion(face);
                var dominantEmotionVi = FaceEmotionService.MapEmotionToVietnamese(dominantEmotion);

                return new FaceEmotionResponse
                {
                    DominantEmotion = dominantEmotionVi,
                    EmotionSentence = FaceEmotionService.GetEmotionSentence(dominantEmotion),
                    AllEmotions = _emotionService.GetAllEmotions(face),
                    AgeRange = $"{face.AgeRange.Low}-{face.AgeRange.High}",
                    Gender = face.Gender?.Value ?? "Unknown",
                    GenderConfidence = face.Gender?.Confidence != null ? Math.Round((decimal)face.Gender.Confidence, 2) : 0,
                    HasSunglasses = face.Sunglasses?.Value ?? false,
                    IsSmiling = face.Smile?.Value ?? false,
                    SmileConfidence = face.Smile?.Confidence != null ? Math.Round((decimal)face.Smile.Confidence, 2) : 0
                };
            }).ToList();

            if (results.Count > 0)
            {
                var userId = GetCurrentUserId();
                if (userId.HasValue)
                {
                    try
                    {
                        var firstEmotion = _emotionService.GetDominantEmotion(faces[0]);
                        var moodType = await _unitOfWork.MoodTypes.GetByNameAsync(firstEmotion.ToUpper());

                        if (moodType != null)
                        {
                            var memberProfile = await _unitOfWork.MembersProfile.GetByUserIdAsync(userId.Value);
                            
                            if (memberProfile != null)
                            {
                                memberProfile.MoodTypesId = moodType.Id;
                                memberProfile.UpdatedAt = DateTime.UtcNow;
                                _unitOfWork.MembersProfile.Update(memberProfile);
                                
                                var moodLog = new capstone_backend.Data.Entities.MemberMoodLog
                                {
                                    MemberId = memberProfile.Id,
                                    MoodTypeId = moodType.Id,
                                    Reason = "Face emotion analysis",
                                    Note = $"Detected: {firstEmotion} - {results[0].EmotionSentence}",
                                    ImageUrl = null,
                                    IsPrivate = true,
                                    CreatedAt = DateTime.UtcNow,
                                    UpdatedAt = DateTime.UtcNow,
                                    IsDeleted = false
                                };
                                
                                await _unitOfWork.MemberMoodLogs.AddAsync(moodLog);
                                await _challengeService.HandleCheckinChallengeProgressAsync(userId.Value);
                                await _unitOfWork.SaveChangesAsync();

                                await UpdateCoupleMoodIfNeeded(memberProfile.Id, moodType.Id);
                            }
                        }
                    }
                    catch
                    {
                    }
                }
            }

            return Ok(ApiResponse<List<FaceEmotionResponse>>.Success(results, $"Phát hiện {results.Count} khuôn mặt"));
        }
        catch (InvalidOperationException ex)
        {
            return StatusCode(403, ApiResponse<object>.Error(ex.Message, 403));
        }
        catch
        {
            return StatusCode(500, ApiResponse<object>.Error("Có lỗi xảy ra khi phân tích ảnh. Vui lòng thử lại.", 500));
        }
    }

    private async Task UpdateCoupleMoodIfNeeded(int memberId, int moodTypeId)
    {
        try
        {
            var coupleProfile = await _unitOfWork.CoupleProfiles.GetByMemberIdAsync(memberId);
            if (coupleProfile == null || coupleProfile.Status != CoupleProfileStatus.ACTIVE.ToString())
                return;

            var partnerId = coupleProfile.MemberId1 == memberId ? coupleProfile.MemberId2 : coupleProfile.MemberId1;
            var partnerProfile = await _unitOfWork.MembersProfile.GetByIdAsync(partnerId);
            
            if (partnerProfile?.MoodTypesId == null)
                return;

            var coupleMoodName = await _moodMappingService.GetCoupleMoodTypeAsync(moodTypeId, partnerProfile.MoodTypesId.Value);
            if (string.IsNullOrEmpty(coupleMoodName))
                return;

            var coupleMoodType = await _unitOfWork.Context.CoupleMoodTypes
                .FirstOrDefaultAsync(cmt => cmt.Name == coupleMoodName && cmt.IsActive == true);
            
            if (coupleMoodType == null)
                return;

            coupleProfile.CoupleMoodTypeId = coupleMoodType.Id;
            coupleProfile.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.CoupleProfiles.Update(coupleProfile);

            var coupleMoodLog = new capstone_backend.Data.Entities.CoupleMoodLog
            {
                CoupleId = coupleProfile.id,
                CoupleMoodTypeId = coupleMoodType.Id,
                Note = $"Auto-generated from member mood update (MemberId: {memberId})",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsDeleted = false
            };

            await _unitOfWork.Context.CoupleMoodLogs.AddAsync(coupleMoodLog);
            await _unitOfWork.SaveChangesAsync();
        }
        catch
        {
        }
    }
}
