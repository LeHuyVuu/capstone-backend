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

        public async Task<int> GetCurrentMaxOrderAsync(int testTypeId, CancellationToken ct = default)
        {
            return await _dbSet
                .Where(q => q.TestTypeId == testTypeId)
                .MaxAsync(q => (int?) q.OrderIndex, ct) ?? 0;
        }
    }
}
