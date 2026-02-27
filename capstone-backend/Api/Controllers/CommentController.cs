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
                var result = await _commentService.GetRepliesAsync(commentId, pageNumber, pageSize);
                return OkResponse(result, "Lấy danh sách trả lời thành công");
            }
            catch (Exception ex)
            {
                return BadRequestResponse(ex.Message);
            }
        }
    }
}
