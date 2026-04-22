using capstone_backend.Business.Interfaces;
using capstone_backend.Data.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace capstone_backend.Api.Controllers;

[ApiController]
[Route("api/geo")]
[Authorize]
public class GeoController : BaseController
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<GeoController> _logger;

    public GeoController(IUnitOfWork unitOfWork, ILogger<GeoController> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    [HttpPut("{memberId:int}")]
    public async Task<IActionResult> UpdateGeo(int memberId, [FromBody] UpdateGeoRequest request)
    {
        try
        {
            if (request == null)
            {
                return BadRequestResponse("Dữ liệu cập nhật vị trí không hợp lệ");
            }

            var memberProfile = await _unitOfWork.MembersProfile.GetByIdAsync(memberId);
            if (memberProfile == null || memberProfile.IsDeleted == true)
            {
                return NotFoundResponse("Không tìm thấy member profile");
            }

            // Only update geo fields as requested.
            memberProfile.HomeLatitude = request.HomeLatitude;
            memberProfile.HomeLongitude = request.HomeLongitude;

            _unitOfWork.MembersProfile.Update(memberProfile);
            await _unitOfWork.SaveChangesAsync();

            return OkResponse(new
            {
                homeLatitude = memberProfile.HomeLatitude,
                homeLongitude = memberProfile.HomeLongitude
            }, "Cập nhật tọa độ thành công");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating geo for member {MemberId}", memberId);
            return InternalServerErrorResponse("Đã xảy ra lỗi khi cập nhật tọa độ");
        }
    }

    [HttpGet("{memberId:int}")]
    public async Task<IActionResult> GetGeo(int memberId)
    {
        try
        {
            var memberProfile = await _unitOfWork.MembersProfile.GetByIdAsync(memberId);
            if (memberProfile == null || memberProfile.IsDeleted == true)
            {
                return NotFoundResponse("Không tìm thấy member profile");
            }

            return OkResponse(new
            {
                homeLatitude = memberProfile.HomeLatitude,
                homeLongitude = memberProfile.HomeLongitude
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting geo for member {MemberId}", memberId);
            return InternalServerErrorResponse("Đã xảy ra lỗi khi lấy tọa độ");
        }
    }

    public class UpdateGeoRequest
    {
        public decimal? HomeLatitude { get; set; }
        public decimal? HomeLongitude { get; set; }
    }
}
