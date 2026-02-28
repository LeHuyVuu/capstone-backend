using AutoMapper;
using capstone_backend.Business.DTOs.Common;
using capstone_backend.Business.DTOs.Moderation;
using capstone_backend.Business.DTOs.Post;
using capstone_backend.Business.Interfaces;
using capstone_backend.Business.Jobs.Comment;
using capstone_backend.Business.Jobs.Like;
using capstone_backend.Business.Jobs.Moderation;
using capstone_backend.Data.Entities;
using capstone_backend.Data.Enums;
using Hangfire;
using Microsoft.EntityFrameworkCore;

namespace capstone_backend.Business.Services
{
    public class CommentService : ICommentService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IModerationService _moderationService;

        public CommentService(IUnitOfWork unitOfWork, IMapper mapper, IModerationService moderationService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _moderationService = moderationService;
        }

        public async Task<CommentResponse> CommentPostAsync(int userId, int postId, CreateCommentRequest request)
        {
            var member = await _unitOfWork.MembersProfile.GetByUserIdAsync(userId);
            if (member == null)
                throw new Exception("Hồ sơ thành viên không tồn tại");

            var post = await _unitOfWork.Posts.GetPostWithIncludeById(postId);
            if (post == null || post.IsDeleted == true)
                throw new Exception("Bài viết không tồn tại");

            var moderationResults = await _moderationService.CheckContentByAIService(new List<string> { request.Content });
            if (moderationResults.Any(r => r.Action == ModerationAction.BLOCK))
                throw new Exception("Nội dung của bạn đã bị hệ thống chặn vì vi phạm tiêu chuẩn cộng đồng");

            Comment? parentComment = null;
            int? rootId = null;
            int? actualParentId = request.ParentId;
            int? targetMemberId = null;
            int level = 1;

            if (request.ParentId.HasValue)
            {
                parentComment = await _unitOfWork.Comments.GetByIdAsync(request.ParentId.Value);
                if (parentComment == null || parentComment.IsDeleted == true || parentComment.PostId != post.Id)
                    throw new Exception("Bình luận cha không hợp lệ");

                targetMemberId = parentComment.AuthorId;

                rootId = parentComment.RootId ?? parentComment.Id;

                // Level
                level = parentComment.Level >= 2 ? 3 : 2;

                if (parentComment.Level >= 3)
                    actualParentId = parentComment.ParentId;
            }

            var comment = new Comment
            {
                AuthorId = member.Id,
                TargetMemberId = targetMemberId,
                PostId = post.Id,
                ParentId = actualParentId,
                RootId = rootId,
                Level = level,
                Content = request.Content,
                Status = CommentStatus.PENDING.ToString(),
            };
            await _unitOfWork.Comments.AddAsync(comment);
            await _unitOfWork.SaveChangesAsync();

            var response = _mapper.Map<CommentResponse>(comment);

            BackgroundJob.Enqueue<IModerationWorker>(j => j.ProcessCommentModerationAsync(comment.Id, moderationResults));

            return response;
        }

        public async Task<int> DeleteCommentAsync(int userId, int commentId)
        {
            var member = await _unitOfWork.MembersProfile.GetByUserIdAsync(userId);
            if (member == null)
                throw new Exception("Hồ sơ thành viên không tồn tại");

            var existingComment = await _unitOfWork.Comments.GetByIdAsync(commentId);
            if (existingComment == null)
                throw new Exception("Bình luận không tồn tại");

            var post = await _unitOfWork.Posts.GetByIdAsync(existingComment.PostId);
            if (post == null)
                throw new Exception("Bài viết không tồn tại");

            if (existingComment.AuthorId != member.Id)
                throw new Exception("Bạn không có quyền xóa bình luận này");

            // Soft delete also deletes all child comments (replies)
            await SoftDeleteCommentAndDescendantsAsync(existingComment, post);

            existingComment.IsDeleted = true;
            var result = await _unitOfWork.SaveChangesAsync();

            BackgroundJob.Enqueue<ICommentWorker>(j => j.RecountPostAsync(post.Id));
            if (existingComment.ParentId.HasValue)
            {
                BackgroundJob.Enqueue<ICommentWorker>(
                    j => j.RecountReplyAsync(existingComment.ParentId.Value));
            }

            return result;
        }

        private async Task SoftDeleteCommentAndDescendantsAsync(Comment comment, Post post)
        {
            // Get all direct children
            var childComments = await _unitOfWork.Comments.GetChildCommentsByParentIdAsync(comment.Id);

            // Recursively delete each child and its descendants
            foreach (var child in childComments)
            {
                if (child.Status == CommentStatus.PUBLISHED.ToString())
                {
                    post.CommentCount = Math.Max(0, (post.CommentCount ?? 0) - 1);
                }
                await SoftDeleteCommentAndDescendantsAsync(child, post);
            }

            // Soft delete the current comment
            comment.IsDeleted = true;
        }

        public async Task<CommentResponse> UpdateCommentAsync(int userId, int commentId, UpdateCommentRequest request)
        {
            var member = await _unitOfWork.MembersProfile.GetByUserIdAsync(userId);
            if (member == null)
                throw new Exception("Hồ sơ thành viên không tồn tại");

            var existingComment = await _unitOfWork.Comments.GetByIdAsync(commentId);
            if (existingComment == null || existingComment.IsDeleted == true)
                throw new Exception("Bình luận không tồn tại");

            var post = await _unitOfWork.Posts.GetPostWithIncludeById(existingComment.PostId);
            if (post == null)
                throw new Exception("Bài viết không tồn tại");

            if (existingComment.AuthorId != member.Id)
                throw new Exception("Bạn không có quyền chỉnh sửa bình luận này");

            var moderationResults = await _moderationService.CheckContentByAIService(new List<string> { request.Content });
            if (moderationResults.Any(r => r.Action == ModerationAction.BLOCK))
                throw new Exception("Nội dung của bạn đã bị hệ thống chặn vì vi phạm tiêu chuẩn cộng đồng");

            existingComment.Content = request.Content;
            existingComment.UpdatedAt = DateTime.UtcNow;
            existingComment.Status = CommentStatus.PENDING.ToString();

            await _unitOfWork.SaveChangesAsync();

            BackgroundJob.Enqueue<IModerationWorker>(j => j.ProcessCommentModerationAsync(existingComment.Id, moderationResults));

            var response = _mapper.Map<CommentResponse>(existingComment);

            return response;
        }

        public async Task<PagedResult<CommentResponse>> GetRepliesAsync(int commentId, int pageNumber, int pageSize)
        {
            var comment = await _unitOfWork.Comments.GetByIdAsync(commentId);
            if (comment == null || comment.IsDeleted == true)
                throw new Exception("Bình luận không tồn tại");

            var (comments, count) = await _unitOfWork.Comments.GetPagedAsync(
                pageNumber,
                pageSize,
                c => c.ParentId == commentId && c.IsDeleted == false && c.Status == CommentStatus.PUBLISHED.ToString(),
                c => c.OrderBy(c => c.CreatedAt),
                c => c.Include(c => c.Author).Include(c => c.TargetMember)
            );

            var items = _mapper.Map<List<CommentResponse>>(comments);
            return new PagedResult<CommentResponse>
            {
                Items = items,
                TotalCount = count,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }

        public async Task<CommentLikeResponse> LikeCommentAsync(int userId, int commentId)
        {
            var member = await _unitOfWork.MembersProfile.GetByUserIdAsync(userId);
            if (member == null)
                throw new Exception("Hồ sơ thành viên không tồn tại");

            var comment = await _unitOfWork.Comments.GetByIdAsync(commentId);
            if (comment == null || comment.IsDeleted == true)
                throw new Exception("Bình luận không tồn tại");

            if (comment.Status != CommentStatus.PUBLISHED.ToString())
                throw new Exception("Bình luận chưa được xuất bản");

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                await _unitOfWork.CommentLikes.AddAsync(new CommentLike
                {
                    CommentId = commentId,
                    MemberId = member.Id
                });

                await _unitOfWork.SaveChangesAsync();

                await _unitOfWork.CommitTransactionAsync();
            }
            catch (Exception)
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw new Exception("Bạn đã like bình luận này rồi");
            }

            BackgroundJob.Enqueue<ILikeWorker>(j => j.RecountCommentLikeAsync(comment.Id));

            return new CommentLikeResponse
            {
                CommentLikeCount = comment.LikeCount.Value + 1,
                IsLikedByMe = true
            };
        }
    }
}
