using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using capstone_backend.Business.Interfaces;

namespace capstone_backend.Business.Services;

/// <summary>
/// Service for AWS S3 file storage operations
/// </summary>
public class S3Service : IS3Service
{
    private readonly IAmazonS3 _s3Client;
    private readonly ILogger<S3Service> _logger;
    private readonly string _bucketName;
    private readonly string _region;

    public S3Service(IAmazonS3 s3Client, ILogger<S3Service> logger, IConfiguration configuration)
    {
        _s3Client = s3Client;
        _logger = logger;
        _bucketName = Environment.GetEnvironmentVariable("AWS_S3_BUCKET_NAME") 
            ?? configuration["AWS:S3:BucketName"] 
            ?? throw new InvalidOperationException("AWS S3 bucket name not configured");
        _region = Environment.GetEnvironmentVariable("AWS_REGION") 
            ?? configuration["AWS:Region"] 
            ?? "ap-southeast-1";
    }

    public async Task<string> UploadFileAsync(IFormFile file, string? folder = null, CancellationToken cancellationToken = default)
    {
        if (file == null || file.Length == 0)
            throw new ArgumentException("File is empty or null");

        // Generate unique filename
        var extension = Path.GetExtension(file.FileName);
        var uniqueFileName = $"{Guid.NewGuid()}{extension}";

        return await UploadFileWithNameAsync(file, Path.GetFileNameWithoutExtension(uniqueFileName), folder, cancellationToken);
    }

    public async Task<string> UploadFileWithNameAsync(IFormFile file, string fileName, string? folder = null, CancellationToken cancellationToken = default)
    {
        if (file == null || file.Length == 0)
            throw new ArgumentException("File is empty or null");

        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException("File name cannot be empty");

        try
        {
            // Build S3 key (path in bucket)
            var extension = Path.GetExtension(file.FileName);
            var sanitizedFileName = SanitizeFileName(fileName);
            var key = string.IsNullOrWhiteSpace(folder)
                ? $"{sanitizedFileName}{extension}"
                : $"{folder.TrimEnd('/')}/{sanitizedFileName}{extension}";

            // Prepare upload request
            var uploadRequest = new TransferUtilityUploadRequest
            {
                InputStream = file.OpenReadStream(),
                Key = key,
                BucketName = _bucketName,
                ContentType = file.ContentType ?? "application/octet-stream",
                CannedACL = S3CannedACL.PublicRead, // Make file publicly accessible
                AutoCloseStream = true
            };

            // Upload using TransferUtility (supports multipart for large files automatically)
            var transferUtility = new TransferUtility(_s3Client);
            await transferUtility.UploadAsync(uploadRequest, cancellationToken);

            // Generate public URL
            var fileUrl = $"https://{_bucketName}.s3.{_region}.amazonaws.com/{key}";

            _logger.LogInformation("File uploaded successfully to S3: {FileUrl}", fileUrl);
            return fileUrl;
        }
        catch (AmazonS3Exception ex)
        {
            _logger.LogError(ex, "AWS S3 error uploading file: {Message}", ex.Message);
            throw new Exception($"Failed to upload file to S3: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading file to S3");
            throw new Exception($"Failed to upload file: {ex.Message}", ex);
        }
    }

    public async Task<bool> DeleteFileAsync(string fileUrl, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(fileUrl))
            throw new ArgumentException("File URL cannot be empty");

        try
        {
            // Extract key from URL
            var key = ExtractKeyFromUrl(fileUrl);

            var deleteRequest = new DeleteObjectRequest
            {
                BucketName = _bucketName,
                Key = key
            };

            await _s3Client.DeleteObjectAsync(deleteRequest, cancellationToken);

            _logger.LogInformation("File deleted successfully from S3: {Key}", key);
            return true;
        }
        catch (AmazonS3Exception ex)
        {
            _logger.LogError(ex, "AWS S3 error deleting file: {Message}", ex.Message);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting file from S3");
            return false;
        }
    }

    /// <summary>
    /// Sanitize filename to remove invalid characters
    /// </summary>
    private string SanitizeFileName(string fileName)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = string.Join("_", fileName.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries));
        return sanitized.Trim();
    }

    /// <summary>
    /// Extract S3 key from full URL
    /// </summary>
    private string ExtractKeyFromUrl(string url)
    {
        // Handle both formats:
        // https://bucket.s3.region.amazonaws.com/folder/file.jpg
        // https://s3.region.amazonaws.com/bucket/folder/file.jpg

        var uri = new Uri(url);
        var path = uri.AbsolutePath.TrimStart('/');

        // If URL format is bucket.s3.region.amazonaws.com, path is the key
        if (uri.Host.StartsWith(_bucketName))
        {
            return path;
        }

        // If URL format is s3.region.amazonaws.com/bucket, remove bucket name from path
        var segments = path.Split('/', 2);
        return segments.Length > 1 ? segments[1] : path;
    }
}
