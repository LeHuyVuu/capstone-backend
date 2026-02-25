using capstone_backend.Business.DTOs.Moderation;
using OpenAI.Moderations;
using System.ClientModel.Primitives;

namespace capstone_backend.Business.Interfaces
{
    public interface IModerationService
    {
        (bool IsValid, string? Message) CheckContent(string content);
        Task<List<ModerationResultDto>> CheckContentByAIService(List<string> inputs);
        Task<ModerationResult> TestAsync(string content);
    }
}
