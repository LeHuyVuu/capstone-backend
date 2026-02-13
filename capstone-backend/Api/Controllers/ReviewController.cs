using capstone_backend.Api.Filters;
using capstone_backend.Business.DTOs.Review;
using capstone_backend.Business.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace capstone_backend.Api.Controllers
{
    [Route("api/[controller]")]
    [Authorize(Roles = "MEMBER, member, VENUEOWNER")]
    [Moderation]
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
        public async Task<IActionResult> SubmitReviewAsync([FromBody] CreateReviewRequest request)
        {
            try
            {

                var userId = GetCurrentUserId();
                var result = await _reviewService.SubmitReviewAsync(userId.Value, request);
                return OkResponse(result, "Đánh giá địa điểm thành công");
            }
            catch (Exception ex)
            {
                return BadRequestResponse(ex.Message);
            }
        }

        /// <summary>
        /// Update a review for a venue location
        /// </summary>
        [HttpPut("{reviewId}/update")]
        public async Task<IActionResult> SubmitReviewAsync(int reviewId, [FromBody] UpdateReviewRequest request)
        {
            try
            {

                var userId = GetCurrentUserId();
                var result = await _reviewService.UpdateReviewAsync(userId.Value, reviewId, request);
                return OkResponse(result, "Thay đổi đánh giá địa điểm thành công");
            }
            catch (Exception ex)
            {
                return BadRequestResponse(ex.Message);
            }
        }

        /// <summary>
        /// Delete a review for a venue location
        /// </summary>
        [HttpDelete("{reviewId}/delete")]
        public async Task<IActionResult> DeleteReviewAsync(int reviewId)
        {
            try
            {
                var userId = GetCurrentUserId();
                var result = await _reviewService.DeleteReviewAsync(userId.Value, reviewId);
                return OkResponse(result, "Xoá đánh giá địa điểm thành công");
            }
            catch (Exception ex)
            {
                return BadRequestResponse(ex.Message);
            }
        }

        /// <summary>
        /// Reply to a review (for Venue Owners)
        /// </summary>
        [HttpPost("{reviewId:int}/reply")]
        public async Task<IActionResult> ReplyToReviewAsync(int reviewId, [FromBody] ReviewReplyRequest request)
        {
            try
            {
                var role = GetCurrentUserRole();
                if (role == null || !role.Equals("VENUEOWNER", StringComparison.OrdinalIgnoreCase))
                {
                    return UnauthorizedResponse("Chỉ chủ địa điểm mới có thể phản hồi đánh giá");
                }

                var userId = GetCurrentUserId();
                var result = await _reviewService.ReplyToReviewAsync(userId.Value, reviewId, request);
                return OkResponse(result, "Phản hồi đánh giá thành công");
            }
            catch (Exception ex)
            {
                return BadRequestResponse(ex.Message);
            }
        }

        /// <summary>
        /// Update review reply (for Venue Owners)
        /// </summary>
        [HttpPut("{reviewId:int}/reply")]
        public async Task<IActionResult> UpdateReplyReviewAsync(int reviewId, [FromBody] ReviewReplyRequest request)
        {
            try
            {
                var role = GetCurrentUserRole();
                if (role == null || !role.Equals("VENUEOWNER", StringComparison.OrdinalIgnoreCase))
                {
                    return UnauthorizedResponse("Chỉ chủ địa điểm mới có thể cập nhật phản hồi đánh giá");
                }

                var userId = GetCurrentUserId();
                var result = await _reviewService.UpdateReplyReviewAsync(userId.Value, reviewId, request);
                return OkResponse(result, "Cập nhật phản hồi đánh giá thành công");
            }
            catch (Exception ex)
            {
                return BadRequestResponse(ex.Message);
            }
        }

        /// <summary>
        /// Delete review reply (for Venue Owners)
        /// </summary>
        [HttpDelete("{reviewId:int}/reply")]
        public async Task<IActionResult> DeleteReplyReviewAsync(int reviewId)
        {
            try
            {
                var role = GetCurrentUserRole();
                if (role == null || !role.Equals("VENUEOWNER", StringComparison.OrdinalIgnoreCase))
                {
                    return UnauthorizedResponse("Chỉ chủ địa điểm mới có thể xoá phản hồi đánh giá");
                }
                var userId = GetCurrentUserId();
                var result = await _reviewService.DeleteReviewReplyAsync(userId.Value, reviewId);
                return OkResponse(result, "Xoá phản hồi đánh giá thành công");
            }
            catch (Exception ex)
            {
                return BadRequestResponse(ex.Message);
            }
        }

        /// <summary>
        /// Like a review
        /// </summary>
        [HttpPost("{reviewId:int}/toggle-like")]
        public async Task<IActionResult> ToggleLikeReviewAsync(int reviewId)
        {
            try
            {
                var userId = GetCurrentUserId();
                var result = await _reviewService.ToggleLikeReviewAsync(userId.Value, reviewId);
                var message = result.IsLiked ? "Thích đánh giá thành công" : "Bỏ thích đánh giá thành công";
                return OkResponse(result, message);
            }
            catch (Exception ex)
            {
                return BadRequestResponse(ex.Message);
            }
        }
    }
}
