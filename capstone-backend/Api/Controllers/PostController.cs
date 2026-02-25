using capstone_backend.Business.DTOs.Post;
using capstone_backend.Business.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace capstone_backend.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class PostController : BaseController
    {
        private readonly IPostService _postService;

        public PostController(IPostService postService)
        {
            _postService = postService;
        }

        /// <summary>
        /// Get feeds for member
        /// </summary>
        [HttpGet("feeds")]
        public async Task<IActionResult> GetFeeds([FromQuery] FeedRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                {
                    return UnauthorizedResponse("User không xác thực");
                }

                var result = await _postService.GetFeedsAsync(userId.Value, request);
                return OkResponse(result);
            }
            catch (Exception ex)
            {
                return BadRequestResponse(ex.Message);
            }
        }

        /// <summary>
        /// Get post details by id
        /// </summary>
        [HttpGet("{postId:int}")]
        public async Task<IActionResult> GetPostDetails([FromQuery] int postId)
        {
            try
            {
                var result = await _postService.GetPostDetailsAsync(postId);
                if (result == null)
                    return NotFoundResponse("Bài viết không tồn tại");

                return OkResponse(result);
            }
            catch (Exception ex)
            {
                return BadRequestResponse(ex.Message);
            }
        }
    }
}
