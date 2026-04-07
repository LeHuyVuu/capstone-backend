using capstone_backend.Business.DTOs.CoupleProfile;
using capstone_backend.Business.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace capstone_backend.Api.Controllers;

[ApiController]
[Route("api/couple-profile")]
[Authorize]
public class CoupleProfileController : BaseController
{
    private readonly ICoupleProfileService _service;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CoupleProfileController> _logger;

    public CoupleProfileController(
        ICoupleProfileService service,
        IUnitOfWork unitOfWork,
        ILogger<CoupleProfileController> logger)
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
            throw new UnauthorizedAccessException("Không tìm thấy ID người dùng trong token");
        }

        // Query MemberId from database using UserId
        var memberProfile = await _unitOfWork.MembersProfile.GetByUserIdAsync(userId);
        if (memberProfile == null)
        {
            throw new UnauthorizedAccessException("Không tìm thấy hồ sơ thành viên của người dùng này");
        }

        return memberProfile.Id;
    }

    /// <summary>
    /// Lấy chi tiết couple profile của member hiện tại
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(object), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> GetCoupleProfileDetail()
    {
        try
        {
            var memberId = await GetCurrentMemberIdAsync();
            var (success, message, data) = await _service.GetCoupleProfileDetailAsync(memberId);

            if (!success)
            {
                return BadRequestResponse(message);
            }

            return OkResponse(data, message);
        }
        catch (UnauthorizedAccessException ex)
        {
            return UnauthorizedResponse(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting couple profile detail");
            return InternalServerErrorResponse("Đã xảy ra lỗi khi lấy thông tin cặp đôi");
        }
    }


    [HttpPut]
    [ProducesResponseType(typeof(object), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> UpdateCoupleProfile([FromBody] UpdateCoupleProfileRequest request)
    {
        try
        {
            var memberId = await GetCurrentMemberIdAsync();
            var (success, message, data) = await _service.UpdateCoupleProfileAsync(memberId, request);

            if (!success)
            {
                return BadRequestResponse(message);
            }

            return OkResponse(data, message);
        }
        catch (Business.Exceptions.BadRequestException ex)
        {
            return BadRequestResponse(ex.Message);
        }
        catch (UnauthorizedAccessException ex)
        {
            return UnauthorizedResponse(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating couple profile");
            return InternalServerErrorResponse("Đã xảy ra lỗi khi cập nhật thông tin cặp đôi");
        }
    }
}
