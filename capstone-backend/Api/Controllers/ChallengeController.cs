using capstone_backend.Business.Common.Constants;
using capstone_backend.Business.DTOs.Challenge;
using capstone_backend.Business.Interfaces;
using capstone_backend.Data.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace capstone_backend.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "ADMIN")]
    public class ChallengeController : BaseController
    {
        private readonly IChallengeService _challengeService;

        public ChallengeController(IChallengeService challengeService)
        {
            _challengeService = challengeService;
        }

        /// <summary>
        /// Get Definition of Challenges
        /// </summary>
        [HttpGet("definitions")]
        public async Task<IActionResult> GetChallengeDefinitions()
        {
            string checkinCode = ChallengeTriggerEvent.CHECKIN.ToString();
            string reviewCode = ChallengeTriggerEvent.REVIEW.ToString();
            string postCode = ChallengeTriggerEvent.POST.ToString();

            var data = new
            {
                TaskTypes = new[]
                {
                    new
                    {
                        Code = checkinCode,
                        Label = "Điểm danh hằng ngày"
                    },

                    new
                    {
                        Code = reviewCode,
                        Label = "Viết đánh giá địa điểm"
                    },

                    new
                    {
                        Code = postCode,
                        Label = "Đăng bài viết"
                    }
                },

                Metrics = new[]
                {
                    new
                    {
                        Code = ChallengeConstants.GoalMetrics.COUNT,
                        Label = "Cộng dồn số lượng (tích luỹ)"
                    },

                    new
                    {
                        Code = ChallengeConstants.GoalMetrics.UNIQUE_LIST,
                        Label = "Số địa điểm khác nhau"
                    },

                    new
                    {
                        Code = ChallengeConstants.GoalMetrics.Streak,
                        Label = "Chuỗi ngày liên tiếp"
                    }
                },

                Rules = new Dictionary<string, object>
                {
                    [checkinCode] = new object[] { },
                    [reviewCode] = new[]
                    {
                        new
                        {
                            Key =  ChallengeConstants.RuleKeys.VENUE_ID,
                            Label = "ID Quán (Bỏ trống nếu quán nào cũng được)",
                            TYPE = "NUMBER"
                        },

                        new
                        {
                            Key = ChallengeConstants.RuleKeys.HAS_IMAGE,
                            Label = "Yêu cầu có hình ảnh trong đánh giá (tuỳ chọn)",
                            TYPE = "BOOLEAN"
                        }
                    },
                    [postCode] = new[]
                    {
                        new
                        {
                            Key = ChallengeConstants.RuleKeys.HASH_TAG,
                            Label = "Hashtag trong bài viết",
                            TYPE = "STRING"
                        },

                        new
                        {
                            Key = ChallengeConstants.RuleKeys.HAS_IMAGE,
                            Label = "Yêu cầu có hình ảnh trong đánh giá (tuỳ chọn)",
                            TYPE = "BOOLEAN"
                        }
                    }
                }
            };

            return OkResponse(data);
        }

        /// <summary>
        /// Create a new Challenge (Admin only)
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateChallenge([FromBody] CreateChallengeRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                {
                    return UnauthorizedResponse();
                }

                var result = await _challengeService.CreateChallengeAsyncV2(userId.Value, request);
                if (result == null)
                {
                    return BadRequestResponse("Failed to create challenge");
                }
                return OkResponse(result, "Create challenge successfully");
            }
            catch (Exception ex)
            {
                return BadRequestResponse(ex.Message);
            }
        }

        /// <summary>
        /// Get all Challenges (Admin only)
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAllChallenges([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                var result = await _challengeService.GetAllChallengesAsync(pageNumber, pageSize);
                if (result == null)
                {
                    return BadRequestResponse("Lấy danh sách thử thách thất bại");
                }

                return OkResponse(result, "Lấy danh sách thử thách thành công");
            }
            catch (Exception ex)
            {
                return BadRequestResponse(ex.Message);
            }
        }
    }
}
