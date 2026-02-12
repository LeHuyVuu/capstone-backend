using capstone_backend.Business.Common.Constants;
using capstone_backend.Business.Interfaces;
using capstone_backend.Data.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace capstone_backend.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
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
            var data = new
            {
                Triggers = new[]
                {
                    new
                    {
                        Code = ChallengeTriggerEvent.CHECKIN.ToString(),
                        Name = "Điểm danh hằng ngày"
                    },

                    new
                    {
                        Code = ChallengeTriggerEvent.REVIEW.ToString(),
                        Name = "Viết đánh giá"
                    },

                    new
                    {
                        Code = ChallengeTriggerEvent.POST.ToString(),
                        Name = "Đăng bài viết"
                    }
                },

                Metrics = new[]
                {
                    new
                    {
                        Code = ChallengeConstants.GoalMetrics.COUNT,
                        Name = "Đếm số lượng"
                    },

                    new
                    {
                        Code = ChallengeConstants.GoalMetrics.Streak,
                        Name = "Chuỗi liên tiếp"
                    }
                },

                Rules = new Dictionary<string, object>
                {
                    ["CHECKIN"] = new object[] { },
                    ["REVIEW"] = new[]
                    {
                        new
                        {
                            Key =  ChallengeConstants.RuleKeys.VENUE_ID,
                            Label = "ID Quán (Bỏ trống nếu quán nào cũng được)",
                            TYPE = "NUMBER"
                        },
                    },
                    ["POST"] = new[]
                    {
                        new
                        {
                            Key = ChallengeConstants.RuleKeys.HASH_TAG,
                            Label = "Hashtag trong bài viết",
                            TYPE = "STRING"
                        },
                    }
                }
            };

            return OkResponse(data);
        }
    }
}
