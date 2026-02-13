using Amazon.S3;
using capstone_backend.Business.Interfaces;
using capstone_backend.Data.Entities;

namespace capstone_backend.Business.Services
{
    public class MediaService : IMediaService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly S3StorageService _s3Service;

        public MediaService(IUnitOfWork unitOfWork, S3StorageService s3Service)
        {
            _unitOfWork = unitOfWork;
            _s3Service = s3Service;
        }

        public async Task<List<string>> UploadMediaAsync(List<IFormFile> files, int userId, string type)
        {
            if (files == null || !files.Any())
                throw new ArgumentException("No files to upload");

            var uploadedUrls = new List<string>();
            try
            {
                foreach (var file in files)
                {
                    if (file == null || file.Length == 0)
                        continue;
                    var url = await _s3Service.UploadFileAsync(file, userId, type);
                    uploadedUrls.Add(url);
                }

                // Save database
                var mediaEntities = uploadedUrls.Select(url => new Media
                {
                    Url = url,
                    MediaType = type,
                    UploaderId = userId
                }).ToList();

                await _unitOfWork.Media.AddRangeAsync(mediaEntities);
                await _unitOfWork.SaveChangesAsync();

                return uploadedUrls;
            }
            catch (AmazonS3Exception ex)
            {
                foreach (var url in uploadedUrls)
                {
                    await _s3Service.DeleteFileByKeyAsync(url);
                }

                throw new Exception("Error uploading media to S3.", ex);
            }
            catch (Exception ex)
            {

                // Cleanup already uploaded files
                foreach (var url in uploadedUrls)
                {
                    await _s3Service.DeleteFileByKeyAsync(url);
                }

                throw;
            }
        }
    }
}
