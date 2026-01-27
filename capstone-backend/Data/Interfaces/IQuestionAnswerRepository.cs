using capstone_backend.Business.DTOs.QuestionAnswer;
using capstone_backend.Business.Interfaces;
using capstone_backend.Data.Entities;

namespace capstone_backend.Data.Interfaces
{
    public interface IQuestionAnswerRepository : IGenericRepository<QuestionAnswer>
    {
        Task<List<QuestionAnswer>> GetAllByQuestionIdsAsync(List<int> questionIds);
        Task<List<QuestionAnswerScoreDto>> GetScoringMapAsync(int testTypeId);
    }
}
