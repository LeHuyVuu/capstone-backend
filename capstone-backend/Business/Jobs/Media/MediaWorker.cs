
using Amazon.S3;
using capstone_backend.Business.Interfaces;
using capstone_backend.Business.Services;
using capstone_backend.Data.Entities;

namespace capstone_backend.Business.Jobs.Media
{
    public class MediaWorker : IMediaWorker
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<MediaWorker> _logger;
        private readonly S3StorageService _s3Service;

        public MediaWorker(ILogger<MediaWorker> logger, IUnitOfWork unitOfWork, S3StorageService s3Service)
        {
            _logger = logger;
            _unitOfWork = unitOfWork;
            _s3Service = s3Service;
        }

        public async Task DeleteMediaFileAsync()
        {
            var deletedMediaList = await _unitOfWork.Media.GetAllDeletedAsync();
            if (deletedMediaList == null || !deletedMediaList.Any())
            {
                _logger.LogInformation("[MEDIA CLEANUP] Deleted media list empty");
                return;
            }

            var successDeleteMedia = new List<Data.Entities.Media>();
            foreach (var media in deletedMediaList)
            {
                try
                {
                    var s3Key = GetS3KeyFromUrl(media.Url);
                    if (string.IsNullOrEmpty(s3Key))
                    {
                        _logger.LogWarning($"Cannot extract S3 key from URL for media ID {media.Id}");
                        continue;
                    }

                    await _s3Service.DeleteFileByKeyAsync(s3Key);
                    successDeleteMedia.Add(media);

                }
                catch (AmazonS3Exception ex)
                {
                    _logger.LogError($"Delete error S3 (AmazonS3Exception) ID {media.Id}: {ex.Message}");
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Unknown error ID {media.Id}: {ex.Message}");
                }
            }

            if (successDeleteMedia.Any())
            {
                _unitOfWork.Media.DeleteRange(successDeleteMedia);
                await _unitOfWork.SaveChangesAsync();
            }
        }

        private string? GetS3KeyFromUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url)) 
                return null;
            try
            {
                var uri = new Uri(url);
                return uri.AbsolutePath.TrimStart('/');
            }
            catch
            {
                return null;
            }
        }
    }
}
