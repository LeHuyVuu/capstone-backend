using capstone_backend.Business.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace capstone_backend.Api.Controllers
{
    [Route("api/couple-challenges")]
    [ApiController]
    [Authorize(Roles = "MEMBER, member")]
    public class CoupleChallengeProfileController : BaseController
    {
        private readonly IChallengeService _challengeService;

        public CoupleChallengeProfileController(IChallengeService challengeService)
        {
            _challengeService = challengeService;
        }

        /// <summary>
        /// Get available challenges for member
        /// </summary>
        [HttpGet("challenges")]
        public async Task<IActionResult> GetAvailableChallenges([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                {
                    return UnauthorizedResponse("User không xác thực");
                }
                var challenges = await _challengeService.GetMemberChallengesAsync(userId.Value, pageNumber, pageSize);
                return OkResponse(challenges, "Lấy danh sách thử thách thành công");
            }
            catch (Exception ex)
            {
                return BadRequestResponse(ex.Message);
            }
        }

        /// <summary>
        /// Get challenge details for member
        /// </summary>
        [HttpGet("challenges/{challengeId}")]
        public async Task<IActionResult> GetChallengeDetails([FromRoute] int challengeId)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                {
                    return UnauthorizedResponse("User không xác thực");
                }
                var challenge = await _challengeService.GetMemberChallengeByIdAsync(userId.Value, challengeId);
                if (challenge == null)
                    return NotFoundResponse("Thử thách không tồn tại");
                return OkResponse(challenge, "Lấy chi tiết thử thách thành công");
            }
            catch (Exception ex)
            {
                return BadRequestResponse(ex.Message);
            }
        }
    }
}
