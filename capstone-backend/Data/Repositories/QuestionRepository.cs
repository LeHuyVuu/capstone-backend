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

        public Task<List<Question>> GetAllByVersionAsync(int version, string role)
        {
            var result = role.ToUpper() == "ADMIN"
                ? _dbSet.Where(q => q.Version == version && q.IsDeleted == false).ToListAsync()
                : _dbSet.Where(q => q.Version == version && q.IsActive == true && q.IsDeleted == false).ToListAsync();

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
                    TotalQuestions = g.Count(),
                    IsActive = g.Any(q => q.IsActive == true)
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
