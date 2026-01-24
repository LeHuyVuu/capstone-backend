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

        public async Task<List<QuestionAnswer>> GetAllByQuestionIdsAsync(
            List<int> questionIds,
            string role)
        {
            var query = _dbSet.Where(a => questionIds.Contains(a.QuestionId));

            if (role != "ADMIN")
                query = query.Where(a => a.IsActive == true);

            return await query.ToListAsync();
        }
    }
}
