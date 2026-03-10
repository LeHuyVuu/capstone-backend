using capstone_backend.Business.DTOs.Moderation;

namespace capstone_backend.Business.Jobs.Moderation
{
    public interface IModerationWorker
    {
        Task ProcessPostModerationAndChallengeAsync(int postId, List<ModerationResultDto> results, int userId, bool hasImage, IEnumerable<string>? hashTags, int? venueId);
        Task ProcessPostModerationAsync(int postId, List<ModerationResultDto> results);
        Task ProcessCommentModerationAsync(int commentId, List<ModerationResultDto> results);

        Task ProcessReviewModerationAndChallengeAsync(int reviewId, List<ModerationResultDto> results, int userId, int? venueId, bool hasImage);
        Task ProcessReviewModerationAsync(int reviewId, List<ModerationResultDto> results);
    }
}
