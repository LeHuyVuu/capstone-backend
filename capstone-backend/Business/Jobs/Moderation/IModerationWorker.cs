using capstone_backend.Business.DTOs.Moderation;

namespace capstone_backend.Business.Jobs.Moderation
{
    public interface IModerationWorker
    {
        Task ProcessPostModerationAsync(int postId, List<ModerationResultDto> results);
        Task ProcessCommentModerationAsync(int commentId, List<ModerationResultDto> results);
    }
}
