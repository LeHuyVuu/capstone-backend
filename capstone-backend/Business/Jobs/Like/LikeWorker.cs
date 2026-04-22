
using capstone_backend.Business.Interfaces;
using capstone_backend.Data.Entities;
using capstone_backend.Data.Enums;
using Microsoft.EntityFrameworkCore;

namespace capstone_backend.Business.Jobs.Like
{
    public class LikeWorker : ILikeWorker
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<LikeWorker> _logger;

        public LikeWorker(IUnitOfWork unitOfWork, ILogger<LikeWorker> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task RecountCommentLikeAsync(int commentId)
        {
            var comment = await _unitOfWork.Comments.GetByIdIncludeAsync(commentId);
            if (comment == null || comment.IsDeleted == true)
                return;

            comment.LikeCount = comment.CommentLikes.Count(cl => cl.CommentId == commentId);

            _unitOfWork.Comments.Update(comment);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task RecountPostLikeAsync(int postId)
        {
            var post = await _unitOfWork.Posts.GetPostWithIncludeById(postId);
            if (post == null)
                return;

            post.LikeCount = post.PostLikes.Count(pl => pl.PostId == postId);

            _unitOfWork.Posts.Update(post);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task RebuildInteractionPointsFromLikesAsync()
        {
            try
            {
                var now = DateTime.UtcNow;
                var seasonStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
                var seasonKey = $"{now.Year}-{now.Month:D2}";

                var couples = await _unitOfWork.Context.CoupleProfiles
                    .Where(c => c.IsDeleted != true && c.Status == CoupleProfileStatus.ACTIVE.ToString())
                    .ToListAsync();

                if (!couples.Any())
                    return;

                var memberIds = couples
                    .SelectMany(c => new[] { c.MemberId1, c.MemberId2 })
                    .Distinct()
                    .ToList();

                var postLikeByAuthor = await _unitOfWork.Context.Set<PostLike>()
                    .Where(pl =>
                        pl.CreatedAt.HasValue &&
                        pl.CreatedAt.Value >= seasonStart &&
                        memberIds.Contains(pl.Post.AuthorId) &&
                        pl.Post.IsDeleted != true &&
                        pl.Post.Status == PostStatus.PUBLISHED.ToString() &&
                        pl.MemberId != pl.Post.AuthorId)
                    .GroupBy(pl => pl.Post.AuthorId)
                    .Select(g => new { MemberId = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(x => x.MemberId, x => x.Count);

                var reviewLikeByAuthor = await _unitOfWork.Context.Set<ReviewLike>()
                    .Where(rl =>
                        rl.CreatedAt.HasValue &&
                        rl.CreatedAt.Value >= seasonStart &&
                        rl.MemberId.HasValue &&
                        rl.Review != null &&
                        memberIds.Contains(rl.Review.MemberId) &&
                        rl.Review.IsDeleted != true &&
                        rl.Review.Status == ReviewStatus.PUBLISHED.ToString() &&
                        rl.MemberId.Value != rl.Review.MemberId)
                    .GroupBy(rl => rl.Review.MemberId)
                    .Select(g => new { MemberId = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(x => x.MemberId, x => x.Count);

                foreach (var couple in couples)
                {
                    var m1Post = postLikeByAuthor.TryGetValue(couple.MemberId1, out var a1) ? a1 : 0;
                    var m2Post = postLikeByAuthor.TryGetValue(couple.MemberId2, out var a2) ? a2 : 0;
                    var m1Review = reviewLikeByAuthor.TryGetValue(couple.MemberId1, out var b1) ? b1 : 0;
                    var m2Review = reviewLikeByAuthor.TryGetValue(couple.MemberId2, out var b2) ? b2 : 0;

                    var totalInteractionPoints = m1Post + m2Post + m1Review + m2Review;

                    couple.RankingPoints = totalInteractionPoints;
                    couple.UpdatedAt = now;
                    _unitOfWork.CoupleProfiles.Update(couple);

                    var leaderboard = await _unitOfWork.Context.Leaderboards.FirstOrDefaultAsync(l =>
                        l.CoupleId == couple.id &&
                        l.SeasonKey == seasonKey &&
                        l.Status == LeaderboardStatus.ACTIVE.ToString());

                    if (leaderboard != null)
                    {
                        leaderboard.TotalPoints = totalInteractionPoints;
                        leaderboard.UpdatedAt = now;
                        _unitOfWork.Context.Leaderboards.Update(leaderboard);
                    }
                }

                await _unitOfWork.SaveChangesAsync();

                await RecalculateMonthlyRankPositionAsync(seasonKey, now);

                _logger.LogInformation("[LIKE WORKER] Rebuilt interaction points from likes for season {SeasonKey}", seasonKey);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[LIKE WORKER] Failed to rebuild interaction points from likes");
            }
        }

        private async Task RecalculateMonthlyRankPositionAsync(string seasonKey, DateTime now)
        {
            var monthlyRows = await _unitOfWork.Context.Leaderboards
                .Where(l => l.PeriodType == "monthly"
                         && l.SeasonKey == seasonKey
                         && l.Status == LeaderboardStatus.ACTIVE.ToString())
                .OrderByDescending(l => l.TotalPoints ?? 0)
                .ThenBy(l => l.UpdatedAt)
                .ThenBy(l => l.Id)
                .ToListAsync();

            if (!monthlyRows.Any())
                return;

            var hasChanges = false;

            for (int i = 0; i < monthlyRows.Count; i++)
            {
                var expectedRank = i + 1; // unique rank: 1,2,3,...
                var row = monthlyRows[i];

                if (row.RankPosition != expectedRank)
                {
                    row.RankPosition = expectedRank;
                    row.UpdatedAt = now;
                    hasChanges = true;
                }
            }

            if (hasChanges)
                await _unitOfWork.SaveChangesAsync();
        }
    }
}
