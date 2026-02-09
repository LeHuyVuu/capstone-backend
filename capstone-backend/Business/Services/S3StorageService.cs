using Amazon.S3;
using Amazon.S3.Model;

namespace capstone_backend.Business.Services;

public class S3StorageService
{
    private readonly IAmazonS3 _s3Client;
    private readonly string _bucketName;
    private readonly string _region;

    public S3StorageService(IAmazonS3 s3Client, IConfiguration config)
    {
        _s3Client = s3Client;

        // Ưu tiên ENV đúng như bạn đang set
        _bucketName =
            Environment.GetEnvironmentVariable("AWS_S3_BUCKET_NAME")
            ?? config["AWS:BucketName"]
            ?? throw new Exception("Missing AWS_S3_BUCKET_NAME (or AWS:BucketName)");

        _region =
            Environment.GetEnvironmentVariable("AWS_REGION")
            ?? config["AWS:Region"]
            ?? "ap-southeast-2";
    }

    public async Task<string> UploadFileAsync(IFormFile file, int userId, string type)
    {
        if (file == null || file.Length == 0)
            throw new ArgumentException("File rỗng");

        var ext = Path.GetExtension(file.FileName);
        var key = $"{type.ToLower()}s/{userId}/{Guid.NewGuid()}{ext}";

        await using var stream = file.OpenReadStream();

        var putRequest = new PutObjectRequest
        {
            BucketName = _bucketName,
            Key = key,
            InputStream = stream,
            ContentType = string.IsNullOrWhiteSpace(file.ContentType)
                ? "application/octet-stream"
                : file.ContentType
        };

        await _s3Client.PutObjectAsync(putRequest);

        // URL chuẩn theo bucket + region (virtual-hosted-style)
        return $"https://{_bucketName}.s3.{_region}.amazonaws.com/{key}";
    }

    public async Task DeleteFileByKeyAsync(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return;

        try
        {
            var deleteRequest = new DeleteObjectRequest
            {
                BucketName = _bucketName,
                Key = key
            };

            await _s3Client.DeleteObjectAsync(deleteRequest);
        }
        catch (AmazonS3Exception ex)
        {

            throw new Exception($"S3 error: {ex.Message}");
        }
    }
}
