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
    public class CommentController : BaseController
    {
        private readonly ICommentService _commentService;

        public CommentController(ICommentService commentService)
        {
            _commentService = commentService;
        }

        /// <summary>
        /// Delete comment
        /// </summary>
        [HttpDelete("{commentId:int}")]
        public async Task<IActionResult> DeleteComment([FromRoute] int commentId)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                {
                    return UnauthorizedResponse("User không xác thực");
                }
                var result = await _commentService.DeleteCommentAsync(userId.Value, commentId);
                if (result <= 0)
                    return NotFoundResponse("Xóa bình luận thất bại");
                return OkResponse(result, "Xóa bình luận thành công");
            }
            catch (Exception ex)
            {
                return BadRequestResponse(ex.Message);
            }
        }

        /// <summary>
        /// Update comment
        /// </summary>
        [HttpPut("{commentId:int}")]
        public async Task<IActionResult> UpdateComment([FromRoute] int commentId, [FromBody] UpdateCommentRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                {
                    return UnauthorizedResponse("User không xác thực");
                }

                var result = await _commentService.UpdateCommentAsync(userId.Value, commentId, request);
                if (result == null)
                    return NotFoundResponse("Chỉnh sửa bình luận thất bại");
                return OkResponse(result, "Chỉnh sửa bình luận thành công");
            }
            catch (Exception ex)
            {
                return BadRequestResponse(ex.Message);
            }
        }

        /// <summary>
        /// Get replies for a comment
        /// </summary>
        [HttpGet("{commentId:int}/replies")]
        public async Task<IActionResult> GetReplies([FromRoute] int commentId, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                {
                    return UnauthorizedResponse("User không xác thực");
                }
                var result = await _commentService.GetRepliesAsync(userId.Value, commentId, pageNumber, pageSize);
                return OkResponse(result, "Lấy danh sách trả lời thành công");
            }
            catch (Exception ex)
            {
                return BadRequestResponse(ex.Message);
            }
        }

        /// <summary>
        /// Like comment
        /// </summary>
        [HttpPost("{commentId:int}/like")]
        public async Task<IActionResult> LikeComment([FromRoute] int commentId)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                {
                    return UnauthorizedResponse("User không xác thực");
                }
                var result = await _commentService.LikeCommentAsync(userId.Value, commentId);
                if (result == null)
                    return NotFoundResponse("Thích bình luận thất bại");
                return OkResponse(result, "Thích bình luận thành công");
            }
            catch (Exception ex)
            {
                return BadRequestResponse(ex.Message);
            }
        }

        /// <summary>
        /// Unlike comment
        /// </summary>
        [HttpDelete("{commentId:int}/unlike")]
        public async Task<IActionResult> UnlikeComment([FromRoute] int commentId)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                {
                    return UnauthorizedResponse("User không xác thực");
                }
                var result = await _commentService.UnlikeCommentAsync(userId.Value, commentId);
                if (result == null)
                    return NotFoundResponse("Bỏ thích bình luận thất bại");
                return OkResponse(result, "Bỏ thích bình luận thành công");
            }
            catch (Exception ex)
            {
                return BadRequestResponse(ex.Message);
            }
        }

        /// <summary>
        /// Get detail of a comment by id
        /// </summary>
        [HttpGet("{commentId:int}")]
        public async Task<IActionResult> GetCommentById([FromRoute] int commentId)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (userId == null)
                {
                    return UnauthorizedResponse("User không xác thực");
                }
                var result = await _commentService.GetCommentByIdAsync(userId.Value, commentId);
                if (result == null)
                    return NotFoundResponse("Không tìm thấy bình luận");
                return OkResponse(result, "Lấy thông tin bình luận thành công");
            }
            catch (Exception ex)
            {
                return BadRequestResponse(ex.Message);
            }
        }
    }
}
