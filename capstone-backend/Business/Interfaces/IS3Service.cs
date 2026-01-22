namespace capstone_backend.Business.Interfaces;

/// <summary>
/// Service interface for AWS S3 file operations
/// </summary>
public interface IS3Service
{
    /// <summary>
    /// Upload file to S3 bucket
    /// </summary>
    /// <param name="file">File to upload</param>
    /// <param name="folder">Optional folder path in bucket (e.g., "avatars", "images/products")</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Full public URL of uploaded file</returns>
    Task<string> UploadFileAsync(IFormFile file, string? folder = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Upload file with custom filename
    /// </summary>
    /// <param name="file">File to upload</param>
    /// <param name="fileName">Custom filename (without extension)</param>
    /// <param name="folder">Optional folder path in bucket</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Full public URL of uploaded file</returns>
    Task<string> UploadFileWithNameAsync(IFormFile file, string fileName, string? folder = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete file from S3 bucket
    /// </summary>
    /// <param name="fileUrl">Full URL or key of file to delete</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if deleted successfully</returns>
    Task<bool> DeleteFileAsync(string fileUrl, CancellationToken cancellationToken = default);
}
