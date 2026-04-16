using AutoMapper;
using AutoMapper.Execution;
using capstone_backend.Business.Common;
using capstone_backend.Business.DTOs.Common;
using capstone_backend.Business.DTOs.Moderation;
using capstone_backend.Business.DTOs.Post;
using capstone_backend.Business.Interfaces;
using capstone_backend.Business.Jobs.Comment;
using capstone_backend.Business.Jobs.Like;
using capstone_backend.Business.Jobs.Moderation;
using capstone_backend.Business.Jobs.Notification;
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
        private readonly IAccessoryService _accessoryService;

        public CommentService(IUnitOfWork unitOfWork, IMapper mapper, IModerationService moderationService, IAccessoryService accessoryService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _moderationService = moderationService;
            _accessoryService = accessoryService;
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
            var accessories = await _accessoryService.GetEquippedAccessoryForMemberAsync(comment.AuthorId);
            response.Author.EquippedAccessories = accessories;

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

        public async Task<PagedResult<CommentResponse>> GetRepliesAsync(int userId, int commentId, int pageNumber, int pageSize)
        {
            var comment = await _unitOfWork.Comments.GetByIdAsync(commentId);
            if (comment == null || comment.IsDeleted == true)
                throw new Exception("Bình luận không tồn tại");

            var member = await _unitOfWork.MembersProfile.GetByUserIdAsync(userId);
            if (member == null)
                throw new Exception("Hồ sơ thành viên không tồn tại");

            var (comments, count) = await _unitOfWork.Comments.GetPagedAsync(
                pageNumber,
                pageSize,
                c => c.ParentId == commentId && c.IsDeleted == false && c.Status == CommentStatus.PUBLISHED.ToString(),
                c => c.OrderBy(c => c.CreatedAt),
                c => c.Include(c => c.Author).ThenInclude(a => a.User).Include(c => c.TargetMember).ThenInclude(tm => tm.User).Include(c => c.CommentLikes)
            );

            var items = _mapper.Map<List<CommentResponse>>(comments);
            var commentById = comments.ToDictionary(c => c.Id);
            foreach(var item in items)
            {
                if (!commentById.TryGetValue(item.Id, out var entity))
                    continue;

                item.IsLikedByMe = entity.CommentLikes?.Any(cl => cl.MemberId == member.Id) == true;
                item.IsOwner = entity.AuthorId == member.Id;

                // Add accessory
                var accessories = await _accessoryService.GetEquippedAccessoryForMemberAsync(entity.AuthorId);
                item.Author.EquippedAccessories = accessories;
                if (entity.TargetMemberId.HasValue)
                {
                    var targetAccessories = await _accessoryService.GetEquippedAccessoryForMemberAsync(entity.TargetMemberId.Value);
                    item.ReplyToMember.EquippedAccessories = targetAccessories;
                }
            }

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

            var comment = await _unitOfWork.Comments.GetByIdIncludeAsync(commentId);
            if (comment == null || comment.IsDeleted == true || comment.Status != CommentStatus.PUBLISHED.ToString())
                throw new Exception("Bình luận không tồn tại");

            var notification = new Notification();
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                await _unitOfWork.CommentLikes.AddAsync(new CommentLike
                {
                    CommentId = commentId,
                    MemberId = member.Id
                });

                await _unitOfWork.SaveChangesAsync();

                // Notify only when not liking own comment
                if (comment.AuthorId != member.Id)
                {
                    notification = new Notification
                    {
                        UserId = comment.Author.UserId,
                        Title = NotificationTemplate.Post.TitleNewLikeComment,
                        Message = NotificationTemplate.Post.GetNewLikeCommentBody(member.FullName),
                        Type = NotificationType.SOCIAL.ToString(),
                        ReferenceId = comment.Id,
                        ReferenceType = ReferenceType.COMMENT.ToString(),
                        IsRead = false
                    };

                    await _unitOfWork.Notifications.AddAsync(notification);
                    await _unitOfWork.SaveChangesAsync();
                }

                await _unitOfWork.CommitTransactionAsync();
            }
            catch (Exception)
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw new Exception("Bạn đã like bình luận này rồi");
            }

            BackgroundJob.Enqueue<ILikeWorker>(j => j.RecountCommentLikeAsync(comment.Id));
            if (comment.AuthorId != member.Id)
            {
                BackgroundJob.Enqueue<INotificationWorker>(j => j.SendPushNotificationAsync(notification.Id));
            }

            return new CommentLikeResponse
            {
                CommentLikeCount = comment.LikeCount.Value + 1,
                IsLikedByMe = true
            };
        }

        public async Task<CommentLikeResponse> UnlikeCommentAsync(int userId, int commentId)
        {
            var member = await _unitOfWork.MembersProfile.GetByUserIdAsync(userId);
            if (member == null)
                throw new Exception("Hồ sơ thành viên không tồn tại");

            var comment = await _unitOfWork.Comments.GetByIdIncludeAsync(commentId);
            if (comment == null || comment.IsDeleted == true)
                throw new Exception("Bình luận không tồn tại");

            if (comment.Status != CommentStatus.PUBLISHED.ToString())
                throw new Exception("Bình luận chưa được xuất bản");

            var existingLike = comment.CommentLikes.FirstOrDefault(cl => cl.MemberId == member.Id);
            if (existingLike == null)
                throw new Exception("Bạn chưa like bình luận này");

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                _unitOfWork.CommentLikes.Delete(existingLike);
                await _unitOfWork.SaveChangesAsync();

                await _unitOfWork.CommitTransactionAsync();
            }
            catch (Exception)
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw new Exception("Có lỗi xảy ra khi bạn bỏ thích bình luận này");
            }

            BackgroundJob.Enqueue<ILikeWorker>(j => j.RecountCommentLikeAsync(comment.Id));
            return new CommentLikeResponse
            {
                CommentLikeCount = Math.Max(0, comment.LikeCount.Value - 1),
                IsLikedByMe = false
            };
        }

        public async Task<CommentResponse> GetCommentByIdAsync(int userId, int commentId)
        {
            var existingComment = await _unitOfWork.Comments.GetByIdIncludeAsync(commentId);
            if (existingComment == null)
                throw new Exception("Bình luận không tồn tại");

            var member = await _unitOfWork.MembersProfile.GetByUserIdAsync(userId);
            if (member == null)
                throw new Exception("Hồ sơ thành viên không tồn tại");

            var response = _mapper.Map<CommentResponse>(existingComment);
            
            response.IsLikedByMe = existingComment.CommentLikes?.Any(cl => cl.MemberId == member.Id) == true;
            response.IsOwner = existingComment.AuthorId == member.Id;

            // Add accessory
            var accessories = await _accessoryService.GetEquippedAccessoryForMemberAsync(existingComment.AuthorId);
            response.Author.EquippedAccessories = accessories;

            return response;
        }

        public async Task<PagedResult<CommentResponse>> GetFlaggedCommentsAsync(int pageNumber, int pageSize)
        {
            var (comments, count) = await _unitOfWork.Comments.GetPagedAsync(
                pageNumber,
                pageSize,
                c => c.IsDeleted == false && c.Status == CommentStatus.FLAGGED.ToString(),
                c => c.OrderBy(c => c.CreatedAt),
                c => c.Include(c => c.Author)
                        .ThenInclude(a => a.User)
                       .Include(c => c.TargetMember)
                        .ThenInclude(tm => tm.User)
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

        public async Task<int> ModerateCommentAsync(int commentId, ModerationRequest request)
        {
            var comment = await _unitOfWork.Comments.GetByIdAsync(commentId);
            if (comment == null || comment.IsDeleted == true)
                throw new Exception("Bình luận không tồn tại");

            if (comment.Status != CommentStatus.FLAGGED.ToString())
                throw new Exception("Bình luận này không cần được kiểm duyệt");

            switch (request.Action)
            {
                case ModerationRequestAction.PUBLISH:
                    comment.Status = CommentStatus.PUBLISHED.ToString();
                    break;
                case ModerationRequestAction.CANCEL:
                    comment.Status = CommentStatus.CANCELLED.ToString();
                    break;
                default:
                    throw new Exception("Action không hợp lệ. Chỉ hỗ trợ PUBLISH hoặc CANCEL");
            }

            _unitOfWork.Comments.Update(comment);
            await _unitOfWork.SaveChangesAsync();

            return commentId;
        }
    }
}
