
using capstone_backend.Business.DTOs.Moderation;
using capstone_backend.Business.Interfaces;
using capstone_backend.Data.Enums;

namespace capstone_backend.Business.Jobs.Moderation
{
    public class ModerationWorker : IModerationWorker
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<ModerationWorker> _logger;

        public ModerationWorker(IUnitOfWork unitOfWork, ILogger<ModerationWorker> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task ProcessModerationAsync(int postId, List<ModerationResultDto> results)
        {
            var post = await _unitOfWork.Posts.GetByIdAsync(postId);
            if (post == null || post.IsDeleted == true)
            {
                return;
            }

            if (results.Any(r => r.Action == ModerationAction.PENDING))
                post.Status = PostStatus.FLAGGED.ToString();
            else
                post.Status = PostStatus.PUBLISHED.ToString();

            _logger.LogInformation($"[MODERATION WORKER] Post ID {postId} moderated with status: {post.Status}");

            await _unitOfWork.SaveChangesAsync();
        }
    }
}
