using capstone_backend.Api.Filters;
using capstone_backend.Business.Common.Constants;
using capstone_backend.Business.DTOs.Post;
using capstone_backend.Business.Interfaces;
using capstone_backend.Business.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace capstone_backend.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "MEMBER, member")]
    public class PostController : BaseController
    {
        private readonly IPostService _postService;
        private readonly ICommentService _commentService;

        public PostController(IPostService postService, ICommentService commentService)
        {
            _postService = postService;
            _commentService = commentService;
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
        public async Task<IActionResult> GetPostDetails([FromRoute] int postId)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                {
                    return UnauthorizedResponse("User không xác thực");
                }

                var result = await _postService.GetPostDetailsAsync(userId.Value, postId);
                if (result == null)
                    return NotFoundResponse("Bài viết không tồn tại");

                return OkResponse(result);
            }
            catch (Exception ex)
            {
                return BadRequestResponse(ex.Message);
            }
        }

        /// <summary>
        /// Create new post
        /// </summary>
        /// <remarks>
        /// Visibity options:
        /// - PUBLIC: Ai cũng có thể xem được
        /// - PRIVATE: Chỉ mình tác giả mới xem đượ
        /// - COUPLE_ONLY: Chỉ người yêu mới xem được
        /// </remarks>
        [Moderation]
        [HttpPost]
        public async Task<IActionResult> CreatePost([FromBody] CreatePostRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                {
                    return UnauthorizedResponse("User không xác thực");
                }

                var result = await _postService.CreatePostAsync(userId.Value, request);
                if (result == null)
                    return BadRequestResponse("Tạo bài viết thất bại");
                return OkResponse(result, "Tạo bài viết thành công");
            }
            catch (Exception ex)
            {
                return BadRequestResponse(ex.Message);
            }
        }

        /// <summary>
        /// Update post by id
        /// </summary>
        /// <remarks>
        /// Visibity options:
        /// - PUBLIC: Ai cũng có thể xem được
        /// - PRIVATE: Chỉ mình tác giả mới xem đượ
        /// - COUPLE_ONLY: Chỉ người yêu mới xem được
        /// </remarks>
        [Moderation]
        [HttpPut("{postId:int}")]
        public async Task<IActionResult> UpdatePost([FromRoute] int postId, [FromBody] UpdatePostRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                {
                    return UnauthorizedResponse("User không xác thực");
                }

                var result = await _postService.UpdatePostAsync(userId.Value, postId, request);
                if (result == null)
                    return NotFoundResponse("Chỉnh sửa bài viết thất bại");
                return OkResponse(result, "Chỉnh sửa bài viết thành công");
            }
            catch (Exception ex)
            {
                return BadRequestResponse(ex.Message);
            }
        }

        /// <summary>
        /// Delete post by id (soft delete)
        /// </summary>
        [HttpDelete("{postId}")]
        public async Task<IActionResult> DeletePost([FromRoute] int postId)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                {
                    return UnauthorizedResponse("User không xác thực");
                }
                var result = await _postService.DeletePostAsync(userId.Value, postId);
                if (result <= 0)
                    return NotFoundResponse("Xóa bài viết thất bại");
                return OkResponse(result, "Xoá bài viết thành công");
            }
            catch (Exception ex)
            {
                return BadRequestResponse(ex.Message);
            }
        }

        /// <summary>
        /// Get topics for post creation/editing - FE dùng để hiển thị dropdown chọn chủ đề
        /// </summary>
        [HttpGet("topics")]
        public IActionResult GetTopics()
        {
            try
            {
                var topics = InterestConstants.All
                    .Select(x => new { x.Key, x.Display, x.Icon })
                    .ToList();
                return OkResponse(topics);
            }
            catch (Exception ex)
            {
                return BadRequestResponse(ex.Message);
            }
        }

        /// <summary>
        /// Like Posts
        /// </summary>
        [HttpPost("{postId:int}/like")]
        public async Task<IActionResult> LikePost([FromRoute] int postId)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                {
                    return UnauthorizedResponse("User không xác thực");
                }
                var result = await _postService.LikePostAsync(userId.Value, postId);
                if (result == null)
                    return NotFoundResponse("Thích bài viết thất bại");
                return OkResponse(result, "Thích bài viết thành công");
            }
            catch (Exception ex)
            {
                return BadRequestResponse(ex.Message);
            }
        }

        /// <summary>
        /// Unlike posts
        /// </summary>
        [HttpDelete("{postId:int}/unlike")]
        public async Task<IActionResult> UnlikePost([FromRoute] int postId)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                {
                    return UnauthorizedResponse("User không xác thực");
                }
                var result = await _postService.UnlikePostAsync(userId.Value, postId);
                if (result == null)
                    return NotFoundResponse("Bỏ thích bài viết thất bại");
                return OkResponse(result, "Bỏ thích bài viết thành công");
            }
            catch (Exception ex)
            {
                return BadRequestResponse(ex.Message);
            }
        }

        /// <summary>
        /// Get comments for a post
        /// </summary>
        [HttpGet("{postId:int}/comments")]
        public async Task<IActionResult> GetComments([FromRoute] int postId, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                {
                    return UnauthorizedResponse("User không xác thực");
                }
                var result = await _postService.GetCommentsPostAsync(userId.Value, postId, pageNumber, pageSize);
                if (result == null)
                    return NotFoundResponse("Bài viết không tồn tại");
                return OkResponse(result);
            }
            catch (Exception ex)
            {
                return BadRequestResponse(ex.Message);
            }
        }

        /// <summary>
        /// Comment posts
        /// </summary>
        [HttpPost("{postId:int}/comment")]
        public async Task<IActionResult> CommentPost([FromRoute] int postId, [FromBody] CreateCommentRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                {
                    return UnauthorizedResponse("User không xác thực");
                }

                var result = await _commentService.CommentPostAsync(userId.Value, postId, request);
                if (result == null)
                    return NotFoundResponse("Bình luận bài viết thất bại");
                return OkResponse(result, "Bình luận bài viết thành công");
            }
            catch (Exception ex)
            {
                return BadRequestResponse(ex.Message);
            }
        }

        /// <summary>
        /// Get post for member's profile
        /// </summary>
        [HttpGet("me")]
        public async Task<IActionResult> GetMyPosts([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                {
                    return UnauthorizedResponse("User không xác thực");
                }
                var result = await _postService.GetPostsMemberProfileAsync(userId.Value, pageNumber, pageSize);
                return OkResponse(result);
            }
            catch (Exception ex)
            {
                return BadRequestResponse(ex.Message);
            }
        }

        /// <summary>
        /// Get share link for a post (non authenticated)
        /// </summary>
        [AllowAnonymous]
        [HttpGet("{postId:int}/share-link")]
        public async Task<IActionResult> GetShareLink([FromRoute] int postId)
        {
            try
            {
                var post = await _postService.GetPostDetailsAnonymousAsync(postId);
                if (post == null)
                    return NotFoundResponse("Bài viết không tồn tại");

                var result = await _postService.GetLinkAsync(postId);
                if (result == null)
                    return NotFoundResponse("Không thể tạo link chia sẻ cho bài viết này");

                return OkResponse(result, "Tạo link chia sẽ thành công");
            }
            catch (Exception ex)
            {
                return BadRequestResponse(ex.Message);
            }
        }

        /// <summary>
        /// Get post details by share link (non authenticated)
        /// </summary>
        [AllowAnonymous]
        [HttpGet("share/{shareCode}")]
        public async Task<IActionResult> GetPostDetailsByShareLink([FromRoute] string shareCode)
        {
            try
            {
                var result = await _postService.GetPostDetailsByShareLinkAsync(shareCode);
                if (result == null)
                    return NotFoundResponse("Bài viết không tồn tại hoặc đã bị ẩn");
                return OkResponse(result);
            }
            catch (Exception ex)
            {
                return BadRequestResponse(ex.Message);
            }
        }
    }
}
