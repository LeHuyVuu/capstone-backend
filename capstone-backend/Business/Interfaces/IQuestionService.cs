using capstone_backend.Business.DTOs.Question;

namespace capstone_backend.Business.Interfaces
{
    public interface IQuestionService
    {
        Task<ImportResult> GenerateQuestionAsync(int testTypeId, Stream csvStream, CancellationToken ct = default);
    }
}
