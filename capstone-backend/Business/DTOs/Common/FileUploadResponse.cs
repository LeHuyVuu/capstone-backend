namespace capstone_backend.Business.DTOs.Common;

/// <summary>
/// Response for file upload
/// </summary>
public class FileUploadResponse
{
    /// <summary>
    /// Full public URL of uploaded file
    /// </summary>
    public string FileUrl { get; set; } = string.Empty;

    /// <summary>
    /// Original filename
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// File size in bytes
    /// </summary>
    public long FileSize { get; set; }

    /// <summary>
    /// Content type / MIME type
    /// </summary>
    public string ContentType { get; set; } = string.Empty;

    /// <summary>
    /// Upload timestamp
    /// </summary>
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
}
