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

        public async Task<IEnumerable<Question>> GetAllByListQuestionIdsAsync(List<int> questionIds)
        {
            return await _dbSet
                .AsNoTracking()
                .Where(q => q.IsDeleted == false && q.IsActive == true && questionIds.Contains(q.Id))
                .Include(q => q.QuestionAnswers
                    .Where(qa => qa.IsDeleted == false && qa.IsActive == true)
                )
                .ToListAsync();
        }

        public Task<List<Question>> GetAllByVersionAsync(int testTypeId, int version)
        {
            var result = _dbSet
                .Where(q => q.TestTypeId == testTypeId && q.Version == version && q.IsDeleted == false)
                .ToListAsync();

            return result;
        }

        public async Task<IEnumerable<Question>> GetAllQuestionsByTestTypeIdAsync(int testTypeId)
        {
            var testTypeCurrentVersion = await GetCurrentVersionAsync(testTypeId);

            return await _dbSet
                .AsNoTracking()
                .Where(q => q.TestTypeId == testTypeId &&
                    q.IsDeleted == false &&
                    q.IsActive == true &&
                    q.Version == testTypeCurrentVersion
                )
                .Include(q => q.QuestionAnswers
                    .Where(qa => qa.IsDeleted == false && qa.IsActive == true)
                )
                .ToListAsync();             
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

        public async Task<Dictionary<int, HashSet<int>>> GetValidStructureAsync(int testTypeId)
        {
            var data = await _dbSet
                .Where(q => q.TestTypeId == testTypeId && q.IsDeleted == false && q.IsActive == true)
                .Select(q => new
                {
                    QuestionId = q.Id,
                    AnswerIds = q.QuestionAnswers.Select(qa => qa.Id).ToList()
                })
                .ToListAsync();

            return data.ToDictionary(
                x => x.QuestionId,
                x => x.AnswerIds.ToHashSet()
            );
        }
    }
}
