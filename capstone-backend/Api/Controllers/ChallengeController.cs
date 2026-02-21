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
    public class ChallengeController : BaseController
    {
        private readonly IChallengeService _challengeService;

        public ChallengeController(IChallengeService challengeService)
        {
            _challengeService = challengeService;
        }

        /// <summary>
        /// Get Definition of Challenges (Admin only)
        /// </summary>
        [Authorize(Roles = "ADMIN")]
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
        /// <remarks>
        /// **Tạo thử thách mới cho hệ thống**
        /// 
        /// ### Các loại TriggerEvent (Loại nhiệm vụ):
        /// | Loại | Mô tả |
        /// |------|-------|
        /// | CHECKIN | Điểm danh hằng ngày |
        /// | REVIEW | Viết đánh giá địa điểm |
        /// | POST | Đăng bài viết |
        /// 
        /// ### Các loại GoalMetric (Cách tính mục tiêu):
        /// | Code | Mô tả |
        /// |------|-------|
        /// | COUNT | Cộng dồn số lượng (tích luỹ) |
        /// | UNIQUE_LIST | Số địa điểm khác nhau |
        /// | STREAK | Chuỗi ngày liên tiếp |
        /// 
        /// ### RuleData theo từng TriggerEvent:
        /// 
        /// **1. CHECKIN:** Không cần RuleData
        /// ```json
        /// "ruleData": null
        /// ```
        /// 
        /// **2. REVIEW:**
        /// ```json
        /// "ruleData": {
        ///     "venue_id": 123,       // (tuỳ chọn) ID quán cụ thể, bỏ trống = quán nào cũng được
        ///     "has_image": true      // (tuỳ chọn) Yêu cầu có hình ảnh
        /// }
        /// ```
        /// 
        /// **3. POST:**
        /// ```json
        /// "ruleData": {
        ///     "hash_tag": "#DateNight",  // (tuỳ chọn) Hashtag bắt buộc trong bài viết
        ///     "has_image": true          // (tuỳ chọn) Yêu cầu có hình ảnh
        /// }
        /// ```
        /// 
        /// ### Ví dụ Request Body:
        /// ```json
        /// {
        ///     "title": "Reviewer chăm chỉ",
        ///     "description": "Viết 5 đánh giá có hình ảnh",
        ///     "triggerEvent": "REVIEW",
        ///     "goalMetric": "COUNT",
        ///     "targetGoal": 5,
        ///     "rewardPoints": 100,
        ///     "startDate": "2026-03-01T00:00:00",
        ///     "endDate": "2026-03-31T23:59:59",
        ///     "ruleData": {
        ///         "has_image": true
        ///     }
        /// }
        /// ```
        /// 
        /// ### Lưu ý:
        /// - **startDate/endDate**: Có thể null (không giới hạn thời gian)
        /// - **targetGoal**: Số lượng cần đạt để hoàn thành thử thách
        /// - **rewardPoints**: Điểm thưởng khi hoàn thành
        /// </remarks>
        [Authorize(Roles = "ADMIN")]
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
                    return BadRequestResponse("Tạo thử thách thất bại");
                }
                return OkResponse(result, "Tạo thử thách thành công");
            }
            catch (Exception ex)
            {
                return BadRequestResponse(ex.Message);
            }
        }

        /// <summary>
        /// Get all Challenges (Admin only)
        /// </summary>
        [Authorize(Roles = "ADMIN")]
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

        /// <summary>
        /// Delete a Challenge by ID (Admin only)
        /// </summary>
        [Authorize(Roles = "ADMIN")]
        [HttpDelete]
        public async Task<IActionResult> DeleteChallenge([FromQuery] int challengeId)
        {
            try
            {
                var result = await _challengeService.DeleteChallengeAsync(challengeId);
                if (result <= 0)
                {
                    return BadRequestResponse("Xoá thử thách thất bại");
                }
                return OkResponse("Xoá thử thách thành công");
            }
            catch (Exception ex)
            {
                return BadRequestResponse(ex.Message);
            }
        }

        /// <summary>
        /// Update a Challenge by ID (Admin only)
        /// </summary>
        [Authorize(Roles = "ADMIN")]
        [HttpPut("{challengeId:int}")]
        public async Task<IActionResult> UpdateChallenge([FromRoute] int challengeId, [FromBody] UpdateChallengeRequest request)
        {
            try
            {
                var result = await _challengeService.UpdateChallengeAsync(challengeId, request);
                if (result == null)
                {
                    return BadRequestResponse("Cập nhật thử thách thất bại");
                }
                return OkResponse(result, "Cập nhật thử thách thành công");
            }
            catch (Exception ex)
            {
                return BadRequestResponse(ex.Message);
            }
        }

        /// <summary>
        /// Get Details of a Challenge by ID (Admin only)
        /// </summary>
        [Authorize(Roles = "ADMIN")]
        [HttpGet("{challengeId:int}")]
        public async Task<IActionResult> GetChallengeById([FromRoute] int challengeId)
        {
            try
            {
                var result = await _challengeService.GetChallengeByIdAsync(challengeId);
                if (result == null)
                {
                    return BadRequestResponse("Lấy thông tin thử thách thất bại");
                }
                return OkResponse(result, "Lấy thông tin thử thách thành công");
            }
            catch (Exception ex)
            {
                return BadRequestResponse(ex.Message);
            }
        }

        /// <summary>
        /// Change Challenge Status (Admin only)
        /// </summary>
        [Authorize(Roles = "ADMIN")]
        [HttpPatch("{challengeId:int}/status")]
        public async Task<IActionResult> ChangeChallengeStatus([FromRoute] int challengeId, [FromQuery] ChallengeStatus newStatus = ChallengeStatus.INACTIVE)
        {
            try
            {
                var result = await _challengeService.ChangeChallengeStatusAsync(challengeId, newStatus.ToString());
                if (result == null)
                {
                    return BadRequestResponse("Cập nhật trạng thái thử thách thất bại");
                }
                return OkResponse(result, "Cập nhật trạng thái thử thách thành công");
            }
            catch (Exception ex)
            {
                return BadRequestResponse(ex.Message);
            }
        }
    }
}
