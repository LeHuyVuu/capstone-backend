
using capstone_backend.Business.DTOs.Moderation;
using capstone_backend.Business.Interfaces;
using capstone_backend.Business.Jobs.Comment;
using capstone_backend.Data.Enums;
using Hangfire;

namespace capstone_backend.Business.Jobs.Moderation
{
    public class ModerationWorker : IModerationWorker
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<ModerationWorker> _logger;
        private readonly IChallengeService _challengeService;

        public ModerationWorker(IUnitOfWork unitOfWork, ILogger<ModerationWorker> logger, IChallengeService challengeService)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _challengeService = challengeService;
        }

        public async Task ProcessCommentModerationAsync(int commentId, List<ModerationResultDto> results)
        {
            var comment = await _unitOfWork.Comments.GetByIdAsync(commentId);
            if (comment == null || comment.IsDeleted == true)
                return;

            if (results.Any(r => r.Action == ModerationAction.PENDING))
                comment.Status = CommentStatus.FLAGGED.ToString();
            else
                comment.Status = CommentStatus.PUBLISHED.ToString();

            _logger.LogInformation($"[MODERATION WORKER] Comment ID {commentId} moderated with status: {comment.Status}");

            await _unitOfWork.SaveChangesAsync();

            BackgroundJob.Enqueue<ICommentWorker>(j => j.RecountPostAsync(comment.PostId));

            if (comment.RootId.HasValue)
            {
                BackgroundJob.Enqueue<ICommentWorker>(j => j.RecountReplyAsync(comment.RootId.Value));
            }

            if (comment.ParentId.HasValue)
            {
                BackgroundJob.Enqueue<ICommentWorker>(j => j.RecountReplyAsync(comment.ParentId.Value));
            }
        }

        public async Task ProcessPostModerationAsync(int postId, List<ModerationResultDto> results)
        {
            var post = await _unitOfWork.Posts.GetByIdAsync(postId);
            if (post == null || post.IsDeleted == true)
                return;

            if (results.Any(r => r.Action == ModerationAction.PENDING))
                post.Status = PostStatus.FLAGGED.ToString();
            else
                post.Status = PostStatus.PUBLISHED.ToString();

            _logger.LogInformation($"[MODERATION WORKER] Post ID {postId} moderated with status: {post.Status}");

            await _unitOfWork.SaveChangesAsync();
        }

        public async Task ProcessReviewModerationAndChallengeAsync(int reviewId, List<ModerationResultDto> results, int userId, int? venueId, bool hasImage)
        {
            var review = await _unitOfWork.Reviews.GetByIdAsync(reviewId);
            if (review == null || review.IsDeleted == true)
                return;

            if (results.Any(r => r.Action == ModerationAction.PENDING))
                review.Status = ReviewStatus.FLAGGED.ToString();
            else
                review.Status = ReviewStatus.PUBLISHED.ToString();

            _logger.LogInformation($"[MODERATION WORKER] Review ID {reviewId} moderated with status: {review.Status}");
            await _unitOfWork.SaveChangesAsync();

            if (review.Status == ReviewStatus.PUBLISHED.ToString())
            {
                // Only process challenge progress if the review is approved
                await _challengeService.HandleReviewChallengeProgressAsync(userId, reviewId, venueId, hasImage);
            }
        }
    }
}
