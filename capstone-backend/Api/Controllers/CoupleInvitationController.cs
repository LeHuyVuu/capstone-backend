using capstone_backend.Business.DTOs.CoupleInvitation;
using capstone_backend.Business.Interfaces;
using capstone_backend.Business.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace capstone_backend.Api.Controllers;

[ApiController]
[Route("api/couple-invitations")]
[Authorize]
public class CoupleInvitationController : ControllerBase
{
    private readonly ICoupleInvitationService _service;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CoupleInvitationController> _logger;

    public CoupleInvitationController(
        ICoupleInvitationService service,
        IUnitOfWork unitOfWork,
        ILogger<CoupleInvitationController> logger)
    {
        _service = service;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    private async Task<int> GetCurrentMemberIdAsync()
    {
        // Get UserId from JWT token
        var userIdClaim = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value 
                         ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
        {
            throw new UnauthorizedAccessException("User ID not found in token");
        }

        // Query MemberId from database using UserId
        var memberProfile = await _unitOfWork.MembersProfile.GetByUserIdAsync(userId);
        if (memberProfile == null)
        {
            throw new UnauthorizedAccessException("Member profile not found for this user");
        }

        return memberProfile.Id;
    }

    /// <summary>
    /// Gửi lời mời ghép đôi cho member khác
    /// </summary>
    [HttpPost("send")]
    public async Task<IActionResult> SendInvitation([FromBody] SendInvitationDirectRequest request)
    {
        try
        {
            var memberId = await GetCurrentMemberIdAsync();
            var (success, message, data) = await _service.SendInvitationDirectAsync(memberId, request);

            if (!success)
            {
                return BadRequest(new { error = message });
            }

            return Ok(new { success = true, message, data });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending invitation");
            return StatusCode(500, new { error = "Đã xảy ra lỗi khi gửi lời mời" });
        }
    }

    /// <summary>
    /// Chấp nhận lời mời ghép đôi
    /// </summary>
    [HttpPut("{id}/accept")]
    public async Task<IActionResult> AcceptInvitation(int id)
    {
        try
        {
            var memberId = await GetCurrentMemberIdAsync();
            var (success, message, data) = await _service.AcceptInvitationAsync(id, memberId);

            if (!success)
            {
                return BadRequest(new { error = message });
            }

            return Ok(new { success = true, message, data });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error accepting invitation {InvitationId}", id);
            return StatusCode(500, new { error = "Đã xảy ra lỗi khi chấp nhận lời mời" });
        }
    }

    /// <summary>
    /// Từ chối lời mời ghép đôi
    /// </summary>
    [HttpPut("{id}/reject")]
    public async Task<IActionResult> RejectInvitation(int id)
    {
        try
        {
            var memberId = await GetCurrentMemberIdAsync();
            var (success, message) = await _service.RejectInvitationAsync(id, memberId);

            if (!success)
            {
                return BadRequest(new { error = message });
            }

            return Ok(new { success = true, message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rejecting invitation {InvitationId}", id);
            return StatusCode(500, new { error = "Đã xảy ra lỗi khi từ chối lời mời" });
        }
    }

    /// <summary>
    /// Hủy lời mời ghép đôi (chỉ sender)
    /// </summary>
    [HttpPut("{id}/cancel")]
    public async Task<IActionResult> CancelInvitation(int id)
    {
        try
        {
            var memberId = await GetCurrentMemberIdAsync();
            var (success, message) = await _service.CancelInvitationAsync(id, memberId);

            if (!success)
            {
                return BadRequest(new { error = message });
            }

            return Ok(new { success = true, message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling invitation {InvitationId}", id);
            return StatusCode(500, new { error = "Đã xảy ra lỗi khi hủy lời mời" });
        }
    }

    /// <summary>
    /// Chia tay với couple hiện tại
    /// </summary>
    /// <remarks>
    /// API này cho phép thành viên trong cặp đôi chủ động chia tay.
    /// 
    /// **Yêu cầu:**
    /// - Phải đang có cặp đôi ACTIVE
    /// - Không cần xác nhận từ phía partner
    /// 
    /// **Kết quả:**
    /// - Couple profile status → SEPARATED
    /// - Cả 2 members → RelationshipStatus = SINGLE
    /// - Giữ lại record trong database để theo dõi lịch sử
    /// 
    /// **Unhappy cases:**
    /// - Chưa có cặp đôi → 400 Bad Request
    /// - Couple đã SEPARATED/INACTIVE → 400 Bad Request
    /// </remarks>
    [HttpPost("breakup")]
    public async Task<IActionResult> Breakup()
    {
        try
        {
            var memberId = await GetCurrentMemberIdAsync();
            var (success, message) = await _service.BreakupAsync(memberId);

            if (!success)
            {
                return BadRequest(new { error = message });
            }

            return Ok(new { success = true, message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during breakup for member {MemberId}", await GetCurrentMemberIdAsync());
            return StatusCode(500, new { error = "Đã xảy ra lỗi khi xử lý chia tay" });
        }
    }

    /// <summary>
    /// Lấy danh sách lời mời nhận được
    /// </summary>
    /// <param name="status">
    /// Filter theo trạng thái (optional):
    /// - PENDING: Lời mời đang chờ phản hồi
    /// - ACCEPTED: Lời mời đã chấp nhận
    /// - REJECTED: Lời mời đã từ chối
    /// - CANCELLED: Lời mời đã bị hủy bởi người gửi
    /// - null/empty: Lấy tất cả
    /// </param>
    /// <param name="page">Trang hiện tại (mặc định: 1)</param>
    /// <param name="pageSize">Số lượng items mỗi trang (mặc định: 20)</param>
    /// <returns>Danh sách lời mời nhận được</returns>
    /// <response code="200">Trả về danh sách lời mời</response>
    /// <response code="401">Chưa đăng nhập hoặc token không hợp lệ</response>
    [HttpGet("received")]
    [ProducesResponseType(typeof(object), 200)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> GetReceivedInvitations(
        [FromQuery] string? status = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            var memberId = await GetCurrentMemberIdAsync();
            var invitations = await _service.GetReceivedInvitationsAsync(memberId, status, page, pageSize);

            return Ok(new
            {
                success = true,
                data = invitations,
                pagination = new
                {
                    page,
                    pageSize,
                    total = invitations.Count
                }
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting received invitations");
            return StatusCode(500, new { error = "Đã xảy ra lỗi khi lấy danh sách lời mời" });
        }
    }

    /// <summary>
    /// Lấy danh sách lời mời đã gửi
    /// </summary>
    /// <param name="status">
    /// Filter theo trạng thái (optional):
    /// - PENDING: Lời mời đang chờ phản hồi
    /// - ACCEPTED: Lời mời đã được chấp nhận
    /// - REJECTED: Lời mời đã bị từ chối
    /// - CANCELLED: Lời mời đã hủy
    /// - null/empty: Lấy tất cả
    /// </param>
    /// <param name="page">Trang hiện tại (mặc định: 1)</param>
    /// <param name="pageSize">Số lượng items mỗi trang (mặc định: 20)</param>
    /// <returns>Danh sách lời mời đã gửi</returns>
    /// <response code="200">Trả về danh sách lời mời</response>
    /// <response code="401">Chưa đăng nhập hoặc token không hợp lệ</response>
    [HttpGet("sent")]
    [ProducesResponseType(typeof(object), 200)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> GetSentInvitations(
        [FromQuery] string? status = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            var memberId = await GetCurrentMemberIdAsync();
            var invitations = await _service.GetSentInvitationsAsync(memberId, status, page, pageSize);

            return Ok(new
            {
                success = true,
                data = invitations,
                pagination = new
                {
                    page,
                    pageSize,
                    total = invitations.Count
                }
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting sent invitations");
            return StatusCode(500, new { error = "Đã xảy ra lỗi khi lấy danh sách lời mời" });
        }
    }

    /// <summary>
    /// Tìm kiếm members để gửi lời mời
    /// Nếu query rỗng, trả về tất cả members (để khám phá)
    /// </summary>
    [HttpGet("search")]
    public async Task<IActionResult> SearchMembers(
        [FromQuery] string? query = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            var memberId = await GetCurrentMemberIdAsync();
            var members = await _service.SearchMembersAsync(query, memberId, page, pageSize);

            return Ok(new
            {
                success = true,
                data = members,
                pagination = new
                {
                    page,
                    pageSize,
                    total = members.Count
                }
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching members");
            return StatusCode(500, new { error = "Đã xảy ra lỗi khi tìm kiếm members" });
        }
    }
}

