using capstone_backend.Business.DTOs.Question;
using capstone_backend.Data.Enums;

namespace capstone_backend.Business.Interfaces
{
    public interface IQuestionService
    {
        Task<ImportResult> GenerateQuestionAsync(int testTypeId, IFormFile file, CancellationToken ct = default);
        Task<List<QuestionResponse>> GetAllQuestionsByVersionAsync(int testTypeId, int version);
        Task<int> ActivateVersionAsync(int testTypeId, int version);
        Task<List<TestQuestionResponse>> GetAllQuestionsForMemberAsync(int userId, int testTypeId, TestMode mode);
    }
}
