using capstone_backend.Business.Interfaces;
using capstone_backend.Data.Entities;

namespace capstone_backend.Data.Interfaces
{
    public interface IMediaRepository : IGenericRepository<Media>
    {
        Task<int> CountByTargetIdAndTypeAsync(int id, string type);
        Task<IEnumerable<Media>> GetByListTargetIdsAsync(List<int> targetIds, string type);
        Task<IEnumerable<Media>> GetByUrlsAsync(List<string> urls);
    }
}
