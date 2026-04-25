
using AutoMapper.Execution;
using capstone_backend.Business.Common;
using capstone_backend.Business.DTOs.Moderation;
using capstone_backend.Business.DTOs.Notification;
using capstone_backend.Business.Interfaces;
using capstone_backend.Business.Jobs.Comment;
using capstone_backend.Business.Jobs.Notification;
using capstone_backend.Business.Jobs.Review;
using capstone_backend.Data.Enums;
using Hangfire;

namespace capstone_backend.Business.Jobs.Moderation
{
    public class ModerationWorker : IModerationWorker
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<ModerationWorker> _logger;
        private readonly IChallengeService _challengeService;
        private readonly INotificationService _notificationService;

        public ModerationWorker(IUnitOfWork unitOfWork, ILogger<ModerationWorker> logger, IChallengeService challengeService, INotificationService notificationService)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _challengeService = challengeService;
            _notificationService = notificationService;
        }

        public async Task ProcessCommentModerationAsync(int commentId, List<ModerationResultDto> results)
        {
            var comment = await _unitOfWork.Comments.GetByIdIncludeWithAllStatusAsync(commentId);
            if (comment == null || comment.IsDeleted == true)
                return;

            if (results.Any(r => r.Action == ModerationAction.PENDING))
                comment.Status = CommentStatus.FLAGGED.ToString();
            else
                comment.Status = CommentStatus.PUBLISHED.ToString();

            _logger.LogInformation($"[MODERATION WORKER] Comment ID {commentId} moderated with status: {comment.Status}");

            await _unitOfWork.SaveChangesAsync();

            if (comment.Status == CommentStatus.PUBLISHED.ToString())
            {
                int? receiverUserId = comment.TargetMember == null ? comment.Post.Author.UserId : comment.TargetMember.UserId;
                if (receiverUserId.HasValue && receiverUserId.Value != comment.Author.UserId)
                {
                    // Create notification
                    var notification = new Data.Entities.Notification
                    {
                        UserId = receiverUserId.Value,
                        Type = NotificationType.SOCIAL.ToString(),
                        ReferenceId = comment.Id,
                        ReferenceType = ReferenceType.COMMENT.ToString(),
                        Title = comment.TargetMember != null
                                ? NotificationTemplate.Post.TitleNewCommentReply
                                : NotificationTemplate.Post.TitleNewComment,
                        Message = comment.TargetMember != null 
                                  ? NotificationTemplate.Post.GetNewCommentReplyBody(comment.Author.FullName ?? "Ai đó")
                                  : NotificationTemplate.Post.GetNewCommentBody(comment.Author.FullName ?? "Ai đó"),
                        IsRead = false
                    };
                    await _unitOfWork.Notifications.AddAsync(notification);
                    await _unitOfWork.SaveChangesAsync();

                    // Push notification to the user in real-time
                    BackgroundJob.Enqueue<INotificationWorker>(j => j.SendPushNotificationAsync(notification.Id));
                }
            }

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

        public async Task ProcessPostModerationAndChallengeAsync(int postId, List<ModerationResultDto> results, int userId, bool hasImage, IEnumerable<string>? hashTags, int? venueId)
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

            if (post.Status == PostStatus.PUBLISHED.ToString())
            {
                await _challengeService.HandlePostChallengeProgressAsync(userId, postId, venueId, hasImage, hashTags);
            }
        }

        public async Task ProcessReviewModerationAsync(int reviewId, List<ModerationResultDto> results)
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
        }

        public async Task ProcessReviewModerationAndChallengeAsync(int reviewId, List<ModerationResultDto> results, int userId, int? venueId, bool hasImage)
        {
            var member = await _unitOfWork.MembersProfile.GetByUserIdAsync(userId);
            if (member == null) 
                return;

            var review = await _unitOfWork.Reviews.GetByIdAsync(reviewId);
            if (review == null || review.IsDeleted == true)
                return;

            if (results.Any(r => r.Action == ModerationAction.PENDING))
                review.Status = ReviewStatus.FLAGGED.ToString();
            else
                review.Status = ReviewStatus.PUBLISHED.ToString();

            _logger.LogInformation($"[MODERATION WORKER] Review ID {reviewId} moderated with status: {review.Status}");
            await _unitOfWork.SaveChangesAsync();

            var venueLocation = await _unitOfWork.VenueLocations.GetByIdWithOwnerAsync(venueId.Value);

            if (review.Status == ReviewStatus.FLAGGED.ToString())
            {
                await NotifyReviewFlaggedAsync(userId, review.Id, review.VenueId);
                return;
            }

            if (review.Status == ReviewStatus.PUBLISHED.ToString())
            {
                // Only process challenge progress if the review is approved
                await _challengeService.HandleReviewChallengeProgressAsync(userId, reviewId, venueId, hasImage);

                BackgroundJob.Enqueue<IReviewWorker>(j => j.EvaluateReviewRelevanceAsync(reviewId));
                BackgroundJob.Enqueue<IReviewWorker>(j => j.RecountReviewAsync(review.VenueId));
                
                // Re-analyze venue tags after new review (IsPenalty might change)
                if (venueId.HasValue)
                {
                    BackgroundJob.Enqueue<IVenueTagAnalysisService>(j => j.AnalyzeVenueTagsAsync(venueId.Value));
                    _logger.LogInformation("[MODERATION WORKER] Enqueued tag analysis for venue {VenueId} after review {ReviewId} published", venueId.Value, reviewId);
                }

                // Send notification
                if (venueLocation != null)
                {
                    var notification = new NotificationRequest
                    {
                        Type = NotificationType.SOCIAL.ToString(),
                        ReferenceId = review.Id,
                        ReferenceType = ReferenceType.REVIEW.ToString(),
                        Title = NotificationTemplate.Review.TitleReceiveNewReview,
                        Message = NotificationTemplate.Review.GetReceiveNewReviewBody(member.FullName, venueLocation.Name)
                    };

                    await _notificationService.SendNotificationAsync(venueLocation.VenueOwner.UserId, notification);
                }
            }
        }

        private async Task NotifyReviewFlaggedAsync(int authorUserId, int reviewId, int venueId)
        {
            try
            {
                var venueName = "địa điểm";
                var venue = await _unitOfWork.VenueLocations.GetByIdAsync(venueId);
                if (venue != null && !string.IsNullOrWhiteSpace(venue.Name))
                {
                    venueName = venue.Name;
                }

                var notification = new Data.Entities.Notification
                {
                    UserId = authorUserId,
                    Type = NotificationType.SOCIAL.ToString(),
                    ReferenceId = reviewId,
                    ReferenceType = ReferenceType.REVIEW.ToString(),
                    Title = NotificationTemplate.Review.TitleReviewFlagged,
                    Message = NotificationTemplate.Review.GetReviewFlaggedBody(venueName),
                    IsRead = false
                };

                await _unitOfWork.Notifications.AddAsync(notification);
                await _unitOfWork.SaveChangesAsync();

                BackgroundJob.Enqueue<INotificationWorker>(j => j.SendPushNotificationAsync(notification.Id));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[MODERATION WORKER] Failed to send flagged-review notification for reviewId={ReviewId}", reviewId);
            }
        }
    }
}
