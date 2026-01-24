using capstone_backend.Business.DTOs.TestType;
using capstone_backend.Business.Interfaces;
using capstone_backend.Data.Entities;

namespace capstone_backend.Data.Interfaces
{
    public interface IQuestionRepository : IGenericRepository<Question>
    {
        Task<List<VersionSummaryDto>> GetAllVersionsAsync(int testTypeId);
        Task<int> GetCurrentVersionAsync(int testTypeId, CancellationToken ct = default);
        Task<List<Question>> GetAllByVersionAsync(int testTypeId, int version);
    }
}
