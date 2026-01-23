using capstone_backend.Business.Interfaces;
using capstone_backend.Data.Context;
using capstone_backend.Data.Entities;
using capstone_backend.Data.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace capstone_backend.Data.Repositories
{
    public class QuestionRepository : GenericRepository<Question>, IQuestionRepository
    {
        public QuestionRepository(MyDbContext context) : base(context)
        {
        }

        public async Task<(int order, int version)> GetCurrentMaxOrderAndVersionAsync(int testTypeId, CancellationToken ct = default)
        {
            var result = await _dbSet
                .Where(q => q.TestTypeId == testTypeId && (q.IsDeleted == false))
                .GroupBy(q => 1)
                .Select(g => new
                {
                    MaxOrder = g.Max(q => q.OrderIndex) ?? 0,
                    MaxVersion = g.Max(q => q.Version) ?? 0
                })
                .FirstOrDefaultAsync(ct);

            return result == null 
                ? (0, 0) 
                : (result.MaxOrder, result.MaxVersion);
        }
    }
}
