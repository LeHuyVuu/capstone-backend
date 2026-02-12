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
                            Label = "Yêu cầu có hình ảnh trong đánh giá",
                            TYPE = "BOOLEAN"
                        }
                    }
                }
            };

            return OkResponse(data);
        }
    }
}
