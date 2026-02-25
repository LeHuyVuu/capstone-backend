using AutoMapper;
using capstone_backend.Business.Common.Constants;
using capstone_backend.Business.DTOs.Post;
using capstone_backend.Business.Interfaces;
using capstone_backend.Data.Entities;
using capstone_backend.Extensions.Common;
using Google.Api.Gax;

namespace capstone_backend.Business.Services
{
    public class PostService : IPostService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        // Weight Config
        private const double W_PERSONALITY = 65;
        private const double W_TREND = 55;
        private const double W_RECENCY = 60;
        private const double W_LOCATION = 45;
        private const double W_CONTEXT = 30;

        public PostService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
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

            return response;
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
