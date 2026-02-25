using capstone_backend.Business.DTOs.Moderation;

namespace capstone_backend.Business.Jobs.Moderation
{
    public interface IModerationWorker
    {
        Task ProcessModerationAsync(int postId, List<ModerationResultDto> results);
    }
}
