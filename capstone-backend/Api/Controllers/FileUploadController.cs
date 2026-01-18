using capstone_backend.Business.DTOs.Common;
using capstone_backend.Business.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace capstone_backend.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class FileUploadController : BaseController
{
    private readonly IS3Service _s3Service;
    private readonly ILogger<FileUploadController> _logger;

    public FileUploadController(IS3Service s3Service, ILogger<FileUploadController> logger)
    {
        _s3Service = s3Service;
        _logger = logger;
    }

    /// <summary>
    /// Upload single file to S3 (supports large files)
    /// </summary>
    /// <returns>File URL and metadata</returns>
    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(100_000_000)] // 100MB limit
    [RequestFormLimits(MultipartBodyLengthLimit = 100_000_000)]
    public async Task<IActionResult> UploadFile(IFormFile file, string? folder = null)
    {
        try
        {
            if (file == null || file.Length == 0)
                return BadRequestResponse("No file provided or file is empty");

            // Validate file size (100MB max)
            const long maxFileSize = 100 * 1024 * 1024; // 100MB
            if (file.Length > maxFileSize)
                return BadRequestResponse($"File size exceeds maximum allowed size of {maxFileSize / 1024 / 1024}MB");

            // Upload to S3
            var fileUrl = await _s3Service.UploadFileAsync(file, folder);

            var response = new FileUploadResponse
            {
                FileUrl = fileUrl,
                FileName = file.FileName,
                FileSize = file.Length,
                ContentType = file.ContentType ?? "application/octet-stream",
                UploadedAt = DateTime.UtcNow
            };

            return OkResponse(response, "File uploaded successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading file");
            return InternalServerErrorResponse($"Failed to upload file: {ex.Message}");
        }
    }

    /// <summary>
    /// Upload multiple files to S3
    /// </summary>
    /// <returns>List of file URLs and metadata</returns>
    [HttpPost("upload-multiple")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(200_000_000)] // 200MB total limit
    [RequestFormLimits(MultipartBodyLengthLimit = 200_000_000)]
    public async Task<IActionResult> UploadMultipleFiles(List<IFormFile> files, string? folder = null)
    {
        try
        {
            if (files == null || files.Count == 0)
                return BadRequestResponse("No files provided");

            // Validate total size
            const long maxTotalSize = 200 * 1024 * 1024; // 200MB
            var totalSize = files.Sum(f => f.Length);
            if (totalSize > maxTotalSize)
                return BadRequestResponse($"Total file size exceeds maximum allowed size of {maxTotalSize / 1024 / 1024}MB");

            var uploadTasks = files.Select(async file =>
            {
                var fileUrl = await _s3Service.UploadFileAsync(file, folder);
                return new FileUploadResponse
                {
                    FileUrl = fileUrl,
                    FileName = file.FileName,
                    FileSize = file.Length,
                    ContentType = file.ContentType ?? "application/octet-stream",
                    UploadedAt = DateTime.UtcNow
                };
            });

            var responses = await Task.WhenAll(uploadTasks);

            return OkResponse(responses, $"{responses.Length} files uploaded successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading multiple files");
            return InternalServerErrorResponse($"Failed to upload files: {ex.Message}");
        }
    }

    /// <summary>
    /// Upload avatar image (optimized for profile pictures)
    /// </summary>
    /// <returns>Avatar URL</returns>
    [HttpPost("upload-avatar")]
    [Consumes("multipart/form-data")]
    [Authorize]
    [RequestSizeLimit(5_000_000)] // 5MB limit for avatars
    public async Task<IActionResult> UploadAvatar(IFormFile file)
    {
        try
        {
            if (file == null || file.Length == 0)
                return BadRequestResponse("No file provided");

            // Validate file type (images only)
            var allowedTypes = new[] { "image/jpeg", "image/jpg", "image/png", "image/gif", "image/webp" };
            if (!allowedTypes.Contains(file.ContentType?.ToLower()))
                return BadRequestResponse("Only image files (JPEG, PNG, GIF, WebP) are allowed for avatars");

            // Validate file size (5MB max for avatars)
            const long maxFileSize = 5 * 1024 * 1024; // 5MB
            if (file.Length > maxFileSize)
                return BadRequestResponse($"Avatar size exceeds maximum allowed size of {maxFileSize / 1024 / 1024}MB");

            var userId = GetCurrentUserId();
            if (userId == null)
                return UnauthorizedResponse();

            // Upload to avatars folder with user ID as filename
            var fileUrl = await _s3Service.UploadFileWithNameAsync(file, $"avatar_{userId}", "avatars");

            var response = new FileUploadResponse
            {
                FileUrl = fileUrl,
                FileName = file.FileName,
                FileSize = file.Length,
                ContentType = file.ContentType ?? "image/jpeg",
                UploadedAt = DateTime.UtcNow
            };

            return OkResponse(response, "Avatar uploaded successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading avatar");
            return InternalServerErrorResponse($"Failed to upload avatar: {ex.Message}");
        }
    }

    /// <summary>
    /// Delete file from S3
    /// </summary>
    /// <param name="fileUrl">Full URL of file to delete</param>
    /// <returns>Success status</returns>
    [HttpDelete("delete")]
    [Authorize]
    public async Task<IActionResult> DeleteFile([FromQuery] string fileUrl)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(fileUrl))
                return BadRequestResponse("File URL is required");

            var deleted = await _s3Service.DeleteFileAsync(fileUrl);

            if (deleted)
                return OkResponse<object?>(null, "File deleted successfully");
            else
                return NotFoundResponse("File not found or already deleted");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting file");
            return InternalServerErrorResponse($"Failed to delete file: {ex.Message}");
        }
    }
}
