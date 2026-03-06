using capstone_backend.Business.DTOs.Challenge;
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

        /// <summary>
        /// Get doing challenges for member
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetDoingChallenges([FromQuery] CoupleChallengeQuery query)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                {
                    return UnauthorizedResponse("User không xác thực");
                }
                var result = await _challengeService.GetMyCoupleChallengesAsync(userId.Value, query);
                if (result == null)
                    return NotFoundResponse("Không tìm thấy thử thách nào đang thực hiện");
                return OkResponse(result, "Lấy danh sách thử thách đang thực hiện thành công");
            }
            catch (Exception ex)
            {
                return BadRequestResponse(ex.Message);
            }
        }

        /// <summary>
        /// Join a challenge for member
        /// </summary>
        [HttpPost("challenges/{challengeId:int}/join")]
        public async Task<IActionResult> JoinChallenge([FromRoute] int challengeId)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                {
                    return UnauthorizedResponse("User không xác thực");
                }
                var result = await _challengeService.JoinChallengeAsync(userId.Value, challengeId);
                if (result == null)
                    return NotFoundResponse("Thử thách không tồn tại hoặc đã tham gia");

                return OkResponse(result, "Tham gia thử thách thành công");
            }
            catch (Exception ex)
            {
                return BadRequestResponse(ex.Message);
            }
        }

        /// <summary>
        /// Leave a challenge for member
        /// </summary>
        [HttpPost("{coupleChallengeId:int}/leave")]
        public async Task<IActionResult> LeaveChallenge([FromRoute] int coupleChallengeId)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                {
                    return UnauthorizedResponse("User không xác thực");
                }
                var result = await _challengeService.LeaveCoupleChallengeAsync(userId.Value, coupleChallengeId);
                if (result <= 0)
                    return NotFoundResponse("Thử thách không tồn tại hoặc chưa tham gia");
                return OkResponse("Rời khỏi thử thách thành công");
            }
            catch (Exception ex)
            {
                return BadRequestResponse(ex.Message);
            }
        }
    }
}
