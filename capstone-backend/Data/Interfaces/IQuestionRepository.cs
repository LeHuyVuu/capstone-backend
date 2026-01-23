using capstone_backend.Business.Interfaces;
using capstone_backend.Data.Entities;

namespace capstone_backend.Data.Interfaces
{
    public interface IQuestionRepository : IGenericRepository<Question>
    {
        Task<(int order, int version)> GetCurrentMaxOrderAndVersionAsync(int testTypeId, CancellationToken ct = default);
    }
}
