using capstone_backend.Api.Models;
using capstone_backend.Business.DTOs.Common;
using capstone_backend.Business.Interfaces;
using capstone_backend.Business.Services;
using capstone_backend.Data.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace capstone_backend.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UploadController : BaseController
{
    private readonly S3StorageService _s3Service;

    public UploadController(S3StorageService s3Service)
    {
        _s3Service = s3Service;
    }

    [HttpPost("")]
    public async Task<IActionResult> Upload(IFormFile file, [FromQuery] MediaType type)
    {
        if (file == null)
            return BadRequestResponse("Không có file nào được chọn.");

        long totalSize = file.Length;
        if (totalSize > 500 * 1024 * 1024)
            return BadRequestResponse("Tổng dung lượng ảnh quá lớn (Tối đa 500MB).");

        var userId = GetCurrentUserId();
        if (userId == null)
            return UnauthorizedResponse();

        var url = await _s3Service.UploadFileAsync(file, userId.Value, type.ToString());

        return Ok(ApiResponse<object>.Success(url, "Tải tệp lên thành công"));
    }
}