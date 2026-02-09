using capstone_backend.Business.DTOs.Review;
using capstone_backend.Business.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace capstone_backend.Api.Controllers
{
    [Route("api/[controller]")]
    [Authorize(Roles = "MEMBER, member")]
    [ApiController]
    public class ReviewController : BaseController
    {
        private readonly IReviewService _reviewService;

        public ReviewController(IReviewService reviewService)
        {
            _reviewService = reviewService;
        }

        /// <summary>
        /// Check-in to a venue location
        /// </summary>
        [HttpPost("check-in-trigger")]
        public async Task<IActionResult> CheckinAsync([FromBody] CheckinRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                var result = await _reviewService.CheckinAsync(userId.Value, request);
                return OkResponse(result, "Bắt đầu check-in thành công");
            }
            catch (Exception ex)
            {
                return BadRequestResponse(ex.Message);
            }
        }

        /// <summary>
        /// Validate a check-in using the check-in history ID
        /// </summary>
        [HttpPost("validate-condition")]
        public async Task<IActionResult> ValidateAsync([FromQuery] int checkInId, [FromBody] CheckinRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                var result = await _reviewService.ValidateCheckinAsync(userId.Value, checkInId, request);

                return OkResponse(result, "Xác thực check-in thành công");
            }
            catch (Exception ex)
            {
                return BadRequestResponse(ex.Message);
            }
        }

        /// <summary>
        /// Submit a review for a venue location
        /// </summary>
        [HttpPost("submit")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> SubmitReviewAsync([FromForm] CreateReviewRequest request)
        {
            try
            {
                if (request.Images != null)
                {
                    if (request.Images.Count > 5)
                        return BadRequestResponse("Tối đa 5 ảnh");

                    foreach (var img in request.Images)
                    {
                        if (img.Length > 5 * 1024 * 1024) // 5MB
                            return BadRequestResponse($"Ảnh {img.FileName} nặng quá (>5MB).");
                    }
                }

                var userId = GetCurrentUserId();
                var result = await _reviewService.SubmitReviewAsync(userId.Value, request);
                return OkResponse(result, "Đánh giá địa điểm thành công");
            }
            catch (Exception ex)
            {
                return BadRequestResponse(ex.Message);
            }
        }
    }
}
