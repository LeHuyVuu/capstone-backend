using capstone_backend.Api.Models;
using capstone_backend.Business.DTOs.Common;
using capstone_backend.Business.Interfaces;
using capstone_backend.Business.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace capstone_backend.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UploadController : ControllerBase
{
    private readonly S3StorageService _s3Service;

    public UploadController(S3StorageService s3Service)
    {
        _s3Service = s3Service;
    }

    [HttpPost("")]
    public async Task<IActionResult> Upload(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("No file uploaded.");

        var url = await _s3Service.UploadFileAsync(file);

        return Ok(ApiResponse<object>.Success(url, "File uploaded successfully"));
    }
}