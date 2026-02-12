using capstone_backend.Api.Models;
using capstone_backend.Business.Services;
using capstone_backend.Data.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace capstone_backend.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MediaController : BaseController
    {
        private readonly S3StorageService _s3Service;

        public MediaController(S3StorageService s3Service)
        {
            _s3Service = s3Service;
        }

        /// <summary>
        /// Upload multiple media files (images/videos) to S3 (max total size 10MB)
        /// </summary>
        [HttpPost("upload")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Upload(List<IFormFile> files, [FromQuery] MediaType type)
        {
            if (files == null || files.Count == 0)
                return BadRequestResponse("Không có file nào được chọn.");

            long totalSize = files.Sum(f => f.Length);
            if (totalSize > 10 * 1024 * 1024)
                return BadRequestResponse("Tổng dung lượng ảnh quá lớn (Tối đa 10MB).");

            var userId = GetCurrentUserId();
            if (userId == null) 
                return UnauthorizedResponse();

            var uploadedUrls = new List<string>();
            foreach (var file in files)
            {
                var url = await _s3Service.UploadFileAsync(file, userId.Value, type.ToString());
                uploadedUrls.Add(url);
            }

            return OkResponse(uploadedUrls, "Tải ảnh lên thành công");
        }
    }
}
