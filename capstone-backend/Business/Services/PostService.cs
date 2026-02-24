using AutoMapper;
using capstone_backend.Business.DTOs.Post;
using capstone_backend.Business.Interfaces;

namespace capstone_backend.Business.Services
{
    public class PostService : IPostService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public PostService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<FeedResponse> GetFeedsAsync(int userId, FeedRequest request)
        {
            // 1. Get interests of the user
            var member = await _unitOfWork.MembersProfile.GetByUserIdAsync(userId);
            if (member == null)
                throw new Exception("Hồ sơ thành viên không tồn tại");

            var memberInterests = string.IsNullOrEmpty(member.Interests) ? new List<string>() : member.Interests.Split(',').ToList();

            // Get base
            var basePosts = await _unitOfWork.Posts.GetPostsByMemberId(userId, request.PageSize, request.Cursor);
            if (!basePosts.Any())
                return new FeedResponse
                {
                    HasMore = false,
                };

            // Next cursor
            var nextCursor = basePosts.Min(p => p.Id);

            // 2. Get base posts (recency)
            // 3. Scoring
            // 4. Diversity re-ranking
            // 5. Mapping
            var response = _mapper.Map<List<PostResponse>>(basePosts);

            return new FeedResponse
            {
                Posts = response,
                NextCursor = nextCursor,
                HasMore = basePosts.Count() == request.PageSize
            };
        }
    }
}
