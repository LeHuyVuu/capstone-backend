using capstone_backend.Business.DTOs.Moderation;
using capstone_backend.Data.Enums;

namespace capstone_backend.Business.Jobs.Moderation
{
    public interface IModerationWorker
    {
        Task ProcessPostModerationAndChallengeAsync(int postId, List<ModerationResultDto> results, int userId, bool hasImage, IEnumerable<string>? hashTags, int? venueId);
        Task ProcessPostModerationAsync(int postId, List<ModerationResultDto> results);
        Task ProcessCommentModerationAsync(int commentId, List<ModerationResultDto> results);

        Task ProcessReviewModerationAndChallengeAsync(int reviewId, List<ModerationResultDto> results, int userId, int? venueId, bool hasImage);
        Task ProcessReviewModerationAsync(int reviewId, List<ModerationResultDto> results);

        Task NotifyResultModerationAsync(int userId, int contentId, ModerationContentType contentType, ModerationRequestAction action);
    }
}
