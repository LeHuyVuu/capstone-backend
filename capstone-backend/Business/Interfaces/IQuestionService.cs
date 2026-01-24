using capstone_backend.Business.DTOs.Question;

namespace capstone_backend.Business.Interfaces
{
    public interface IQuestionService
    {
        Task<ImportResult> GenerateQuestionAsync(int testTypeId, IFormFile file, CancellationToken ct = default);
        Task<List<QuestionResponse>> GetAllQuestionsByVersionAsync(int testTypeId, int version);
    }
}
