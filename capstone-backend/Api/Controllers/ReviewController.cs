using capstone_backend.Business.DTOs.Review;
using capstone_backend.Business.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace capstone_backend.Api.Controllers
{
    [Route("api/[controller]")]
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
        [HttpPost("check-in")]
        public async Task<IActionResult> CheckinAsync(CheckinRequest request)
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

        ///// <summary>
        ///// Validate a check-in using the check-in history ID
        ///// </summary>
        //[HttpPost("validate")]

    }
}
