using Amazon.Rekognition.Model;
using AutoMapper;
using capstone_backend.Business.Common.Constants;
using capstone_backend.Business.DTOs.Common;
using capstone_backend.Business.DTOs.Moderation;
using capstone_backend.Business.DTOs.Post;
using capstone_backend.Business.Interfaces;
using capstone_backend.Business.Jobs.Like;
using capstone_backend.Business.Jobs.Moderation;
using capstone_backend.Data.Entities;
using capstone_backend.Data.Enums;
using capstone_backend.Extensions.Common;
using Google.Api.Gax;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using NanoidDotNet;
using OpenAI.Moderations;

namespace capstone_backend.Business.Services
{
    public class PostService : IPostService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IModerationService _moderationService;

        // Weight Config
        private const double W_PERSONALITY = 65;
        private const double W_TREND = 55;
        private const double W_RECENCY = 60;
        private const double W_LOCATION = 45;
        private const double W_CONTEXT = 30;

        public PostService(IUnitOfWork unitOfWork, IMapper mapper, IModerationService moderationService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _moderationService = moderationService;
        }

        public async Task<PostResponse> GetPostDetailsAsync(int userId, int postId)
        {
            var member = await _unitOfWork.MembersProfile.GetByUserIdAsync(userId);
            if (member == null)
                throw new Exception("Hồ sơ thành viên không tồn tại");

            var post = await _unitOfWork.Posts.GetPostWithIncludeById(postId);
            if (post == null || post.IsDeleted == true)
                throw new Exception("Bài viết không tồn tại");

            var response = _mapper.Map<PostResponse>(post);
            response.IsLikedByMe = post.PostLikes.Any(pl => pl.MemberId == member.Id);
            response.IsOwner = post.AuthorId == member.Id;
            response.Author.Avatar = post.Author.User.AvatarUrl;

            return response;
        }

        public async Task<PostResponse> CreatePostAsync(int userId, CreatePostRequest request)
        {
            // Validate media
            if (request.MediaPayload != null && request.MediaPayload.Count > 4)
            {
                throw new Exception("Bạn chỉ có thể đính kèm tối đa 4 media cho mỗi bài viết");
            }

            var hasImage = request.MediaPayload != null && request.MediaPayload.Any();

            var member = await _unitOfWork.MembersProfile.GetByUserIdAsync(userId);
            if (member == null)
                throw new Exception("Hồ sơ thành viên không tồn tại");

            var toCheck = new List<string> { request.Content };
            toCheck.AddRange(request.MediaPayload.Select(m => m.Url));
            var moderationResults = await _moderationService.CheckContentByAIService(toCheck);

            if (moderationResults.Any(r => r.Action == ModerationAction.BLOCK))
                throw new Exception("Nội dung của bạn đã bị hệ thống chặn vì vi phạm tiêu chuẩn cộng đồng");

            var post = _mapper.Map<Post>(request);
            post.AuthorId = member.Id;
            post.Status = PostStatus.PENDING.ToString();

            await _unitOfWork.Posts.AddAsync(post);
            await _unitOfWork.SaveChangesAsync();

            BackgroundJob.Enqueue<IModerationWorker>(j => j.ProcessPostModerationAndChallengeAsync(post.Id, moderationResults, userId, hasImage, request.HashTags, null));

            var response = _mapper.Map<PostResponse>(post);
            response.IsOwner = true;
            response.IsLikedByMe = false;
            return response;
        }

        public async Task<PostResponse> UpdatePostAsync(int userId, int postId, UpdatePostRequest request)
        {
            // Validate media
            if (request.MediaPayload != null && request.MediaPayload.Count > 4)
            {
                throw new Exception("Bạn chỉ có thể đính kèm tối đa 4 media cho mỗi bài viết");
            }

            var member = await _unitOfWork.MembersProfile.GetByUserIdAsync(userId);
            if (member == null)
                throw new Exception("Hồ sơ thành viên không tồn tại");

            var existingPost = await _unitOfWork.Posts.GetPostWithIncludeById(postId);
            if (existingPost == null)
                throw new Exception("Bài viết không tồn tại");

            if (existingPost.AuthorId != member.Id)
                throw new Exception("Bạn không có quyền chỉnh sửa bài viết này");

            if (existingPost.Status == PostStatus.CANCELLED.ToString())
                throw new Exception("Bài viết đã bị hủy, không thể chỉnh sửa");

            var toCheck = new List<string> { request.Content };
            toCheck.AddRange(request.MediaPayload.Select(m => m.Url));
            var moderationResults = await _moderationService.CheckContentByAIService(toCheck);

            if (moderationResults.Any(r => r.Action == ModerationAction.BLOCK))
                throw new Exception("Nội dung của bạn đã bị hệ thống chặn vì vi phạm tiêu chuẩn cộng đồng");

            // Update fields
            existingPost = _mapper.Map(request, existingPost);
            await _unitOfWork.SaveChangesAsync();

            BackgroundJob.Enqueue<IModerationWorker>(j => j.ProcessPostModerationAsync(existingPost.Id, moderationResults));

            var response = _mapper.Map<PostResponse>(existingPost);
            response.IsOwner = true;
            response.IsLikedByMe = existingPost.PostLikes.Any(pl => pl.MemberId == member.Id);
            return response;
        }

        public async Task<int> DeletePostAsync(int userId, int postId)
        {
            var member = await _unitOfWork.MembersProfile.GetByUserIdAsync(userId);
            if (member == null)
                throw new Exception("Hồ sơ thành viên không tồn tại");

            var existingPost = await _unitOfWork.Posts.GetPostWithIncludeById(postId);
            if (existingPost == null)
                throw new Exception("Bài viết không tồn tại");

            if (existingPost.AuthorId != member.Id)
                throw new Exception("Bạn không có quyền chỉnh sửa bài viết này");

            if (existingPost.Status == PostStatus.CANCELLED.ToString())
                throw new Exception("Bài viết đã bị hủy, không thể chỉnh sửa");

            existingPost.IsDeleted = true;
            return await _unitOfWork.SaveChangesAsync();
        }

        public async Task<PostLikeResponse> LikePostAsync(int userId, int postId)
        {
            var member = await _unitOfWork.MembersProfile.GetByUserIdAsync(userId);
            if (member == null)
                throw new Exception("Hồ sơ thành viên không tồn tại");

            var post = await _unitOfWork.Posts.GetPostWithIncludeById(postId);
            if (post == null || post.IsDeleted == true)
                throw new Exception("Bài viết không tồn tại");

            if (post.Visibility != PostVisibility.PUBLIC.ToString() && post.Status != PostStatus.PUBLISHED.ToString())
                throw new Exception("Bài viết không hợp lệ để like");

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                await _unitOfWork.PostLikes.AddAsync(new PostLike
                {
                    MemberId = member.Id,
                    PostId = post.Id
                });
                await _unitOfWork.SaveChangesAsync();

                await _unitOfWork.CommitTransactionAsync();
            }
            catch (Exception ex)
            {
                // Rollback if any error occurs
                await _unitOfWork.RollbackTransactionAsync();
                throw new Exception("Bạn đã like bài viết này rồi");
            }

            BackgroundJob.Enqueue<ILikeWorker>(j => j.RecountPostLikeAsync(post.Id));

            return new PostLikeResponse
            {
                PostLikeCount = post.LikeCount.Value + 1,
                IsLikedByMe = true
            };
        }

        public async Task<PostLikeResponse> UnlikePostAsync(int userId, int postId)
        {
            var member = await _unitOfWork.MembersProfile.GetByUserIdAsync(userId);
            if (member == null)
                throw new Exception("Hồ sơ thành viên không tồn tại");

            var post = await _unitOfWork.Posts.GetPostWithIncludeById(postId);
            if (post == null || post.IsDeleted == true)
                throw new Exception("Bài viết không tồn tại");

            if (post.Visibility != PostVisibility.PUBLIC.ToString() && post.Status != PostStatus.PUBLISHED.ToString())
                throw new Exception("Bài viết không hợp lệ để unlike");

            var existingLike = post.PostLikes.FirstOrDefault(pl => pl.MemberId == member.Id);
            if (existingLike == null)
                throw new Exception("Bạn chưa like bài viết này");

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                _unitOfWork.PostLikes.Delete(existingLike);
                await _unitOfWork.SaveChangesAsync();

                await _unitOfWork.CommitTransactionAsync();
            }
            catch (Exception ex)
            {
                // Rollback if any error occurs
                await _unitOfWork.RollbackTransactionAsync();
                throw new Exception("Có lỗi xảy ra khi bạn bỏ thích bài viết này");
            }

            BackgroundJob.Enqueue<ILikeWorker>(j => j.RecountPostLikeAsync(post.Id));

            return new PostLikeResponse
            {
                PostLikeCount = Math.Max(0, post.LikeCount.Value - 1),
                IsLikedByMe = false
            };
        }

        public async Task<PagedResult<CommentResponse>> GetCommentsPostAsync(int userId, int postId, int pageNumber = 1, int pageSize = 10)
        {
            var post = await _unitOfWork.Posts.GetByIdAsync(postId);
            if (post == null || post.IsDeleted == true)
                throw new Exception("Bài viết không tồn tại");

            var member = await _unitOfWork.MembersProfile.GetByUserIdAsync(userId);
            if (member == null)
                throw new Exception("Hồ sơ thành viên không tồn tại");

            var (comments, count) = await _unitOfWork.Comments.GetPagedAsync(
                    pageNumber,
                    pageSize,
                    c => c.Post.Id == postId && 
                         c.IsDeleted == false && 
                         c.Post.Visibility == PostVisibility.PUBLIC.ToString() && 
                         c.Post.Status == PostStatus.PUBLISHED.ToString() && 
                         c.ParentId == null && c.RootId == null,
                    c => c.OrderByDescending(c => c.CreatedAt),
                    c => c.Include(c => c.Author).Include(c => c.CommentLikes)
                );

            var items = _mapper.Map<List<CommentResponse>>(comments);
            var commentById = comments.ToDictionary(x => x.Id);
            foreach (var item in items)
            {
                if (!commentById.TryGetValue(item.Id, out var entity))
                    continue;

                item.IsLikedByMe = entity.CommentLikes?.Any(cl => cl.MemberId == member.Id) == true;

                item.IsOwner = entity.AuthorId == member.Id;
            }


            var pagedResult = new PagedResult<CommentResponse>
            {
                Items = items,
                TotalCount = count,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            return pagedResult;
        }

        public async Task<PostResponse> GetPostDetailsAnonymousAsync(int postId)
        {
            var post = await _unitOfWork.Posts.GetPostWithIncludeById(postId);
            if (post == null || post.IsDeleted == true)
                throw new Exception("Bài viết không tồn tại");

            if (post.Visibility != PostVisibility.PUBLIC.ToString() || post.Status != PostStatus.PUBLISHED.ToString())
                throw new Exception("Bài viết không tồn tại");

            var response = _mapper.Map<PostResponse>(post);

            response.IsLikedByMe = false;
            response.IsOwner = false;
            response.Author.Avatar = post.Author.User.AvatarUrl;

            return response;
        }

        public async Task<ShareLinkResponse> GetLinkAsync(int postId)
        {
            var post = await _unitOfWork.Posts.GetByIdAsync(postId);
            if (post == null || post.IsDeleted == true)
                throw new Exception("Bài viết không tồn tại");

            if (post.Visibility != PostVisibility.PUBLIC.ToString() || post.Status != PostStatus.PUBLISHED.ToString())
                throw new Exception("Bài viết không tồn tại");

            if (string.IsNullOrEmpty(post.ShareCode))
            {
                bool isUnique = false;
                string newCode = "";

                while (!isUnique)
                {
                    newCode = Nanoid.Generate(size: 10);
                    var existing = await _unitOfWork.Posts.GetByShareCodeAsync(newCode);

                    if (existing == null)
                    {
                        isUnique = true;
                    }
                }

                post.ShareCode = newCode;

                _unitOfWork.Posts.Update(post);
                await _unitOfWork.SaveChangesAsync();
            }

            return new ShareLinkResponse
            {
                ShareCode = post.ShareCode,
                ShareLinkUrl = $"{Environment.GetEnvironmentVariable("FE_URL")}/share/p/{post.ShareCode}"
            };
        }

        public async Task<PostResponse> GetPostDetailsByShareLinkAsync(string shareCode)
        {

            var post = await _unitOfWork.Posts.GetByShareCodeAsync(shareCode);
            if (post == null || post.IsDeleted == true)
                throw new Exception("Bài viết không tồn tại");

            if (post.Visibility != PostVisibility.PUBLIC.ToString() || post.Status != PostStatus.PUBLISHED.ToString())
                throw new Exception("Bài viết không tồn tại");

            var response = _mapper.Map<PostResponse>(post);

            response.IsLikedByMe = false;
            response.IsOwner = false;
            response.Author.Avatar = post.Author.User.AvatarUrl;
            return response;
        }

        public async Task<List<PostResponse>> GetPostsMemberProfileAsync(int userId, int pageNumber, int pageSize)
        {
            var user = await _unitOfWork.Users.GetByIdAsync(userId);
            if (user == null)
                throw new Exception("Người dùng không tồn tại");

            var member = await _unitOfWork.MembersProfile.GetByUserIdAsync(userId);
            if (member == null)
                throw new Exception("Hồ sơ thành viên không tồn tại");

            var (posts, count) = await _unitOfWork.Posts.GetPagedAsync(
                    pageNumber,
                    pageSize,
                    p => p.AuthorId == member.Id && p.IsDeleted == false,
                    p => p.OrderByDescending(p => p.CreatedAt),
                    p => p.Include(p => p.PostLikes).Include(p => p.Author)
                );
            var response = _mapper.Map<List<PostResponse>>(posts);
            response.ForEach(r =>
            {
                r.IsLikedByMe = posts.First(p => p.Id == r.Id).PostLikes.Any(pl => pl.MemberId == member.Id);
                r.IsOwner = true;

                r.Author.Avatar = user.AvatarUrl;
            });
            return response;
        }

        public async Task<FeedResponse> GetFeedsAsync(int userId, FeedRequest request)
        {
            var nowVn = TimezoneUtil.ToVietNamTime(DateTime.UtcNow);

            // 1. Get Candidate Posts
            var member = await _unitOfWork.MembersProfile.GetByUserIdAsync(userId);
            if (member == null) 
                throw new Exception("Hồ sơ thành viên không tồn tại");

            var memberInterests = string.IsNullOrEmpty(member.Interests)
                ? new List<string>()
                : member.Interests.Split(',').ToList();

            var basePosts = await _unitOfWork.Posts.GetPostsByMemberId(userId, request.PageSize, request.Cursor); 
            if (!basePosts.Any())
                return new FeedResponse
                {
                    HasMore = false
                };

            var nextCursor = basePosts.Min(p => p.Id);

            // 2. Scoring
            var contextKeys = GetContextualInterests(nowVn);
            var scoredItems = basePosts.Select(p => new ScoredPost
            {
                Data = p,
                TotalScore = CalculateScore(p, request, memberInterests, contextKeys, nowVn, out double pScore, out double tscore, out double rScore),
                PersonalityScore = pScore,
                TrendScore = tscore,
                RecencyScore = rScore
            })
            .ToList();

            // 3. Discovery Mixer
            var finalItems = DiscoveryMixer(scoredItems, request.PageSize);

            // 4. Mapping and Delivery
            var scoredMap = scoredItems.ToDictionary(s => s.Data.Id, s => s.TotalScore);
            var response = finalItems.Select(p =>
            {
                var dto = _mapper.Map<PostFeedResponse>(p);
                if (scoredMap.TryGetValue(p.Id, out var scored))
                    dto.TotalScore = Math.Round(scored, 2);

                dto.IsLikedByMe = p.PostLikes.Any(pl => pl.MemberId == member.Id);
                dto.IsOwner = p.AuthorId == member.Id;

                dto.Author.Avatar = p.Author.User.AvatarUrl;

                return dto;
            })
            .ToList();

            return new FeedResponse
            {
                Posts = response,
                NextCursor = nextCursor,
                HasMore = basePosts.Count() == request.PageSize
            };
        }

        private double CalculateScore(
            Post post, 
            FeedRequest request, 
            List<string> memberInterests, 
            List<string> contextKeys, 
            DateTime now, 
            out double pScore,
            out double tScore,
            out double rScore)
        {
            double total = 0;
            var hoursOld = Math.Max(0.5, (now - post.CreatedAt.Value.ToUniversalTime()).TotalHours);

            // Get Topics
            var postTopics = post.Topic ?? new List<string>();

            // 1 & 6 Personality & Preferences
            var matchCount = memberInterests.Count(child => postTopics.Contains(InterestConstants.GetParent(child)));
            pScore = matchCount > 0 ? (W_PERSONALITY * 0.5) + (matchCount * 15) : 0;
            total += pScore;

            // TODO: 2. Location

            // 3. Context Buff
            if (postTopics.Intersect(contextKeys).Any())
                total += W_CONTEXT;

            // 4. Recency
            rScore = W_RECENCY / Math.Pow((hoursOld + 2), 0.5);
            total += rScore;

            // 5. Trend (count based on like/comment per hour)
            double velocity = (post.LikeCount.Value * 2.0 + post.CommentCount.Value * 5.0) / hoursOld;
            tScore = Math.Min(W_TREND, velocity * 10);
            total += tScore;

            // Quality content
            if (post.MediaPayload?.Any() == true)
                total += 10;

            return total;
        }

        private List<Post> DiscoveryMixer(List<ScoredPost> scoredItems, int pageSize)
        {
            var result = new List<Post>();
            var pool = scoredItems.OrderByDescending(x => x.TotalScore).ToList();

            // Bucket for mix
            var personalBucket = scoredItems.OrderByDescending(x => x.PersonalityScore).ToList();
            var trendingBucket = scoredItems.OrderByDescending(x => x.TrendScore).ToList();
            var discoveryBucket = scoredItems.OrderByDescending(x => x.RecencyScore).ToList();

            for (int i = 0; i < pageSize && pool.Any(); i++)
            {
                ScoredPost selected = null;

                if (i % 3 == 0)
                    selected = PopBest(personalBucket, result);
                else if (i % 3 == 1)
                    selected = PopBest(trendingBucket, result);
                else
                    selected = PopBest(discoveryBucket, result);

                selected ??= PopBest(pool, result);

                if (selected != null)
                {
                    result.Add(selected.Data);
                    pool.RemoveAll(x => x.Data.Id == selected.Data.Id);
                }
            }

            return result;
        }

        private ScoredPost PopBest(List<ScoredPost> source, List<Post> current)
        {

            // Get rid of continous author 
            var match = source.FirstOrDefault(s =>
                !current.Any(c => c.Id == s.Data.Id) &&
                current.LastOrDefault()?.AuthorId != s.Data.AuthorId);

            if (match != null)
                source.Remove(match);

            return match;
        }

        private List<string> GetContextualInterests(DateTime now) 
        { 
            var keys = new List<string>(); 
            int hour = now.Hour; 
            if ((hour >= 11 && hour <= 13) || (hour >= 18 && hour <= 21)) 
                keys.Add("date-food"); 
            if (hour >= 22 || hour <= 2) 
                keys.Add("deep-talk"); 
            if (now.DayOfWeek == DayOfWeek.Saturday || now.DayOfWeek == DayOfWeek.Sunday) 
                keys.Add("experiences"); 
            
            return keys; 
        }

        private class ScoredPost
        {
            public Post Data { get; set; }
            public double TotalScore { get; set; }
            public double PersonalityScore { get; set; }
            public double TrendScore { get; set; }
            public double RecencyScore { get; set; }
        }
    }
}
