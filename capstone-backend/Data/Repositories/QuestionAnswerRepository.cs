using capstone_backend.Business.DTOs.QuestionAnswer;
using capstone_backend.Data.Context;
using capstone_backend.Data.Entities;
using capstone_backend.Data.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace capstone_backend.Data.Repositories
{
    public class QuestionAnswerRepository : GenericRepository<QuestionAnswer>, IQuestionAnswerRepository
    {
        public QuestionAnswerRepository(MyDbContext context) : base(context)
        {
        }

        public async Task<List<QuestionAnswer>> GetAllByQuestionIdsAsync(List<int> questionIds)
        {
            return await _dbSet
                .Where(a => questionIds.Contains(a.QuestionId) && a.IsDeleted == false)
                .ToListAsync();
        }

        public async Task<List<QuestionAnswerScoreDto>> GetScoringMapAsync(int testTypeId)
        {
            return await _dbSet
                .AsNoTracking()
                .Where(qa => qa.Question.TestTypeId == testTypeId && qa.IsActive == true && qa.IsDeleted == false)
                .Select(g => new QuestionAnswerScoreDto
                {
                    AnswerId = g.Id,
                    ScoreKey = g.ScoreKey,
                    ScoreValue = g.ScoreValue.Value
                })
                .ToListAsync();
        }
    }
}
