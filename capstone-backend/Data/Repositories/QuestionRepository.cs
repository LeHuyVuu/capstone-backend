using capstone_backend.Business.DTOs.TestType;
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

        public Task<List<Question>> GetAllByVersionAsync(int testTypeId, int version)
        {
            var result = _dbSet
                .Where(q => q.TestTypeId == testTypeId && q.Version == version && q.IsDeleted == false)
                .ToListAsync();

            return result;
        }

        public async Task<List<VersionSummaryDto>> GetAllVersionsAsync(int testTypeId)
        {
            return await _dbSet
                .AsNoTracking()
                .Where(q => q.TestTypeId == testTypeId && q.IsDeleted == false)
                .GroupBy(q => q.Version)
                .Select(g => new VersionSummaryDto
                {
                    Version = g.Key.Value,
                    TotalQuestions = g.Count()
                })
                .OrderByDescending(x => x.Version)
                .ToListAsync();
        }

        public async Task<int> GetCurrentVersionAsync(int testTypeId, CancellationToken ct = default)
        {
            return await _dbSet
                .Where(q => q.TestTypeId == testTypeId && q.IsDeleted == false)
                .MaxAsync(q => q.Version, ct) ?? 0;
        }
    }
}
