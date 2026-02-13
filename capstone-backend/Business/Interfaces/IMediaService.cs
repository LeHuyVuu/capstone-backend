using capstone_backend.Data.Enums;

namespace capstone_backend.Business.Interfaces
{
    public interface IMediaService
    {
        Task<List<string>> UploadMediaAsync(List<IFormFile> files, int userId, string type);
    }
}
