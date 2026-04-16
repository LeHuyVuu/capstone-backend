using Amazon.S3.Model.Internal.MarshallTransformations;
using AutoMapper;
using capstone_backend.Business.Common;
using capstone_backend.Business.DTOs.Accessory;
using capstone_backend.Business.DTOs.Common;
using capstone_backend.Business.DTOs.Moderation;
using capstone_backend.Business.DTOs.Review;
using capstone_backend.Business.DTOs.VenueLocation;
using capstone_backend.Business.Interfaces;
using capstone_backend.Business.Jobs.Moderation;
using capstone_backend.Business.Jobs.Review;
using capstone_backend.Data.Entities;
using capstone_backend.Data.Enums;
using capstone_backend.Extensions.Common;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using static capstone_backend.Business.Services.VenueLocationService;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace capstone_backend.Business.Services
{
    public class ReviewService : IReviewService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly S3StorageService _s3Service;
        private readonly IModerationService _moderationService;
        private readonly IAccessoryService _accessoryService;

        public ReviewService(IUnitOfWork unitOfWork, IMapper mapper, S3StorageService s3Service, IModerationService moderationService, IAccessoryService accessoryService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _s3Service = s3Service;
            _moderationService = moderationService;
            _accessoryService = accessoryService;
        }

        public async Task<int> CheckinAsync(int userId, CheckinRequest request)
        {
            var member = await _unitOfWork.MembersProfile.GetByUserIdAsync(userId);
            if (member == null)
                throw new Exception("Không tìm thấy hồ sơ thành viên");

            var venue = await _unitOfWork.VenueLocations.GetActiveByIdAsync(request.VenueLocationId);
            if (venue == null)
                throw new Exception("Không tìm thấy địa điểm");

            if (!venue.Latitude.HasValue || !venue.Longitude.HasValue)
                throw new Exception("Địa điểm không có tọa độ hợp lệ");

            var couple = await _unitOfWork.CoupleProfiles.GetActiveCoupleByMemberIdAsync(member.Id);
            int? coupleProfileId = couple?.id;

            // Check if review already exists
            var hasReview = await _unitOfWork.Reviews.HasMemberReviewedVenueAsync(member.Id, request.VenueLocationId, coupleProfileId);
            if (hasReview)
                throw new Exception("Bạn đã đánh giá địa điểm này rồi");

            var lastCheckin = await _unitOfWork.CheckInHistories.GetLatestByMemberIdAndVenueIdAsync(member.Id, request.VenueLocationId);

            if (lastCheckin != null)
            {
                var timeSinceLastCheckin = DateTime.UtcNow - lastCheckin.CreatedAt;
                if (timeSinceLastCheckin.Value.TotalMinutes < 10 && lastCheckin.IsValid == null)
                {
                    throw new InvalidOperationException("Bạn vừa check-in rồi, hãy đợi thông báo xác thực nhé!");
                }
            }

            var distance = GeoCalculator.CalculateDistance(
                request.Latitude,
                request.Longitude,
                venue.Latitude.Value,
                venue.Longitude.Value
            );

            if (distance > 0.1)
                throw new Exception("Bạn đang ở quá xa địa điểm để có thể check-in");

            // Save check in
            var checkIn = new CheckInHistory
            {
                MemberId = member.Id,
                VenueId = request.VenueLocationId,
                Latitude = request.Latitude,
                Longitude = request.Longitude,
                IsValid = null, // Invalid until validated
            };

            await _unitOfWork.CheckInHistories.AddAsync(checkIn);
            await _unitOfWork.SaveChangesAsync();

            // Notify after 10 minutes to validate check-in
            BackgroundJob.Schedule<IReviewWorker>(
                worker => worker.SendReviewNotificationAsync(checkIn.Id),
                TimeSpan.FromMinutes(10)
            );

            return checkIn.Id;
        }

        public async Task<int> DeleteReviewAsync(int userId, int reviewId)
        {
            var member = await _unitOfWork.MembersProfile.GetByUserIdAsync(userId);
            if (member == null)
                throw new Exception("Không tìm thấy hồ sơ thành viên");

            var review = await _unitOfWork.Reviews.GetByIdAndMemberIdAsync(reviewId, member.Id);
            if (review == null)
                throw new Exception("Không tìm thấy đánh giá hợp lệ");

            review.IsDeleted = true;

            var mediaList = await _unitOfWork.Media.GetByTargetIdAndTypeAsync(review.Id, ReferenceType.REVIEW.ToString());
            foreach (var media in mediaList)
            {
                media.IsDeleted = true;
                _unitOfWork.Media.Update(media);
            }

            _unitOfWork.Reviews.Update(review);

            return await _unitOfWork.SaveChangesAsync();
        }

        public async Task<int> DeleteReviewReplyAsync(int userId, int reviewId)
        {
            var venueOwner = await _unitOfWork.VenueOwnerProfiles.GetByUserIdAsync(userId);
            if (venueOwner == null)
                throw new Exception("Không tìm thấy hồ sơ chủ địa điểm");

            var review = await _unitOfWork.Reviews.GetByIdAsync(reviewId);
            if (review == null)
                throw new Exception("Không tìm thấy đánh giá hợp lệ");

            var venue = await _unitOfWork.VenueLocations.GetByIdAsync(review.VenueId);
            if (venue == null || venue.VenueOwnerId != venueOwner.Id)
                throw new Exception("Bạn không có quyền xoá phản hồi đánh giá này");

            var existingReply = await _unitOfWork.ReviewReplies.GetByReviewId(review.Id);
            if (existingReply == null)
                throw new Exception("Không tìm thấy phản hồi đánh giá hợp lệ");

            _unitOfWork.ReviewReplies.Delete(existingReply);
            return await _unitOfWork.SaveChangesAsync();
        }

        public async Task<int> ReplyToReviewAsync(int userId, int reviewId, ReviewReplyRequest request)
        {
            var venueOwner = await _unitOfWork.VenueOwnerProfiles.GetByUserIdAsync(userId);
            if (venueOwner == null)
                throw new Exception("Không tìm thấy hồ sơ chủ địa điểm");

            var review = await _unitOfWork.Reviews.GetByIdAsync(reviewId);
            if (review == null)
                throw new Exception("Không tìm thấy đánh giá hợp lệ");

            var venue = await _unitOfWork.VenueLocations.GetByIdAsync(review.VenueId);
            if (venue == null || venue.VenueOwnerId != venueOwner.Id)
                throw new Exception("Bạn không có quyền phản hồi đánh giá này");

            var existingReply = await _unitOfWork.ReviewReplies.GetByReviewId(review.Id);
            if (existingReply != null)
                throw new Exception("Đã tồn tại phản hồi cho đánh giá này");

            var reply = _mapper.Map<ReviewReply>(request);
            reply.ReviewId = review.Id;
            reply.UserId = userId;

            await _unitOfWork.ReviewReplies.AddAsync(reply);
            return await _unitOfWork.SaveChangesAsync();
        }

        public async Task<int> SubmitReviewAsync(int userId, CreateReviewRequest request)
        {
            if (request.Images != null && request.Images.Count > 3)
                throw new Exception("Bạn chỉ có thể tải lên tối đa 3 hình ảnh cho mỗi đánh giá");

            var member = await _unitOfWork.MembersProfile.GetByUserIdAsync(userId);
            if (member == null)
                throw new Exception("Không tìm thấy hồ sơ thành viên");

            var venue = await _unitOfWork.VenueLocations.GetActiveByIdAsync(request.VenueLocationId);
            if (venue == null)
                throw new Exception("Không tìm thấy địa điểm");

            var couple = await _unitOfWork.CoupleProfiles.GetActiveCoupleByMemberIdAsync(member.Id);
            int? currentCoupleId = couple?.id;

            // Check if review already exists
            var hasReview = await _unitOfWork.Reviews.HasMemberReviewedVenueAsync(member.Id, request.VenueLocationId, currentCoupleId);
            if (hasReview)
                throw new Exception("Bạn đã đánh giá địa điểm này rồi");

            var checkIn = await _unitOfWork.CheckInHistories.GetByIdAsync(request.CheckInId);
            if (checkIn == null || checkIn.MemberId != member.Id || checkIn.VenueId != request.VenueLocationId)
                throw new Exception("Không tìm thấy lịch sử check-in hợp lệ");

            if (checkIn.IsValid != true)
                throw new Exception("Lịch sử check-in chưa được xác thực, không thể đánh giá địa điểm");

            // Moderation
            var toCheck = new List<string> { request.Content };
            if (request.Images != null && request.Images.Any())
                toCheck.AddRange(request.Images);
            var moderationResults = await _moderationService.CheckContentByAIService(toCheck);

            if (moderationResults.Any(r => r.Action == ModerationAction.BLOCK))
                throw new Exception("Nội dung của bạn đã bị hệ thống chặn vì vi phạm tiêu chuẩn cộng đồng");

            var review = _mapper.Map<Review>(request);
            review.MemberId = member.Id;
            review.CoupleProfileId = currentCoupleId;
            review.VenueId = request.VenueLocationId;
            review.Status = ReviewStatus.PENDING.ToString();
            review.IsAnonymous = request.IsAnonymous;
            review.IsMatched = request.IsMatched;

            checkIn.IsValid = false;

            _unitOfWork.CheckInHistories.Update(checkIn);
            await _unitOfWork.Reviews.AddAsync(review);
            await _unitOfWork.SaveChangesAsync();

            var hasImage = false;
            // Handle images
            if (request.Images != null && request.Images.Any())
            {
                var mediaList = await _unitOfWork.Media.GetByUrlsAsync(request.Images);
                foreach (var media in mediaList)
                {
                    if (media.UploaderId != userId || media.TargetId != null || media.TargetType != null)
                        throw new Exception("Ảnh không hợp lệ");

                    media.TargetId = review.Id;
                    media.TargetType = ReferenceType.REVIEW.ToString();
                    _unitOfWork.Media.Update(media);
                }

                hasImage = true;
            }
            await _unitOfWork.SaveChangesAsync();

            BackgroundJob.Enqueue<IModerationWorker>(j => j.ProcessReviewModerationAndChallengeAsync(review.Id, moderationResults, userId, review.VenueId, hasImage));

            return review.Id;
        }

        private bool CalculateIsMatched(CoupleProfile? couple, VenueLocation venue)
        {
            if (couple == null || venue.VenueLocationTags == null || !venue.VenueLocationTags.Any())
                return false;

            var coupleMoodTypeId = couple.CoupleMoodTypeId;
            var couplePersonalityTypeId = couple.CouplePersonalityTypeId;

            // check if venue have any tag match with couple
            var isMatched = venue.VenueLocationTags
                .Where(vlt => vlt.IsDeleted == false && vlt.LocationTag != null)
                .Any(vlt =>
                    (coupleMoodTypeId.HasValue && vlt.LocationTag!.CoupleMoodTypeId == coupleMoodTypeId) ||
                    (couplePersonalityTypeId.HasValue && vlt.LocationTag!.CouplePersonalityTypeId == couplePersonalityTypeId)
                );

            return isMatched;
        }

        public async Task<ReviewLikeResponse> ToggleLikeReviewAsync(int userId, int reviewId)
        {
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var member = await _unitOfWork.MembersProfile.GetByUserIdAsync(userId);
                if (member == null) throw new Exception("Không tìm thấy hồ sơ thành viên");

                var review = await _unitOfWork.Reviews.GetByIdAsync(reviewId);
                if (review == null) throw new Exception("Không tìm thấy đánh giá");

                var isLiked = false;
                var existingLike = await _unitOfWork.ReviewLikes.GetByReviewIdAndMemberIdAsync(reviewId, member.Id);

                if (existingLike != null)
                {
                    _unitOfWork.ReviewLikes.Delete(existingLike);
                    await _unitOfWork.SaveChangesAsync();
                }
                else
                {
                    var newLike = new ReviewLike
                    {
                        ReviewId = reviewId,
                        MemberId = member.Id
                    };

                    try
                    {
                        await _unitOfWork.ReviewLikes.AddAsync(newLike);
                        await _unitOfWork.SaveChangesAsync();
                    }
                    catch (DbUpdateException)
                    {
                        throw new Exception("Bạn đã like đánh giá này rồi");
                    }

                    isLiked = true;

                    // Cộng điểm ranking cho couple của author review
                    if (review.MemberId != member.Id)
                    {
                        var authorCouple = await _unitOfWork.CoupleProfiles.GetActiveCoupleByMemberIdAsync(review.MemberId);
                        if (authorCouple != null)
                        {
                            var now = DateTime.UtcNow;
                            var seasonKey = $"{now.Year}-{now.Month:D2}";

                            authorCouple.InteractionPoints += 1;
                            authorCouple.UpdatedAt = DateTime.UtcNow;
                            _unitOfWork.CoupleProfiles.Update(authorCouple);

                            var leaderboard = await _unitOfWork.Context.Leaderboards.FirstOrDefaultAsync(
                                    l => l.CoupleId == authorCouple.id &&
                                    l.SeasonKey == seasonKey &&
                                    l.Status == LeaderboardStatus.ACTIVE.ToString()
                                );

                            if (leaderboard != null)
                            {
                                leaderboard.TotalPoints = (leaderboard.TotalPoints ?? 0) + 1;
                                leaderboard.UpdatedAt = DateTime.UtcNow;
                                _unitOfWork.Context.Leaderboards.Update(leaderboard);
                            }

                            await _unitOfWork.SaveChangesAsync();
                        }
                    }
                }

                var realCount = await _unitOfWork.ReviewLikes.CountAsync(x => x.ReviewId == reviewId);

                review.LikeCount = realCount;
                review.UpdatedAt = DateTime.UtcNow;
                _unitOfWork.Reviews.Update(review);
                await _unitOfWork.SaveChangesAsync();

                await _unitOfWork.CommitTransactionAsync();

                return new ReviewLikeResponse
                {
                    IsLiked = isLiked,
                    LikeCount = realCount
                };
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }

        public async Task<int> UpdateReplyReviewAsync(int userId, int reviewId, ReviewReplyRequest request)
        {
            var venueOwner = await _unitOfWork.VenueOwnerProfiles.GetByUserIdAsync(userId);
            if (venueOwner == null)
                throw new Exception("Không tìm thấy hồ sơ chủ địa điểm");

            var review = await _unitOfWork.Reviews.GetByIdAsync(reviewId);
            if (review == null)
                throw new Exception("Không tìm thấy đánh giá hợp lệ");

            var venue = await _unitOfWork.VenueLocations.GetByIdAsync(review.VenueId);
            if (venue == null || venue.VenueOwnerId != venueOwner.Id)
                throw new Exception("Bạn không có quyền cập nhật phản hồi đánh giá này");

            var existingReply = await _unitOfWork.ReviewReplies.GetByReviewId(review.Id);
            if (existingReply == null)
                throw new Exception("Không tìm thấy phản hồi đánh giá hợp lệ");

            existingReply.Content = request.Content;
            _unitOfWork.ReviewReplies.Update(existingReply);
            return await _unitOfWork.SaveChangesAsync();
        }

        public async Task<int> UpdateReviewAsync(int userId, int reviewId, UpdateReviewRequest request)
        {
            var member = await _unitOfWork.MembersProfile.GetByUserIdAsync(userId);
            if (member == null)
                throw new Exception("Không tìm thấy hồ sơ thành viên");

            var venue = await _unitOfWork.VenueLocations.GetByIdAsync(request.VenueLocationId);
            if (venue == null)
                throw new Exception("Không tìm thấy địa điểm");

            var review = await _unitOfWork.Reviews.GetByIdAndMemberIdAsync(reviewId, member.Id);
            if (review == null)
                throw new Exception("Không tìm thấy đánh giá hợp lệ");

            review.Rating = request.Rating;
            review.Content = request.Content;
            review.IsAnonymous = request.IsAnonymous;

            if (request.DeletedImageUrls != null && request.DeletedImageUrls.Any())
            {
                var imagesToDelete = await _unitOfWork.Media.GetByUrlsAsync(request.DeletedImageUrls);
                foreach (var img in imagesToDelete)
                {
                    img.IsDeleted = true;
                    _unitOfWork.Media.Update(img);
                }
            }

            int existingImageCount = await _unitOfWork.Media.CountByTargetIdAndTypeAsync(review.Id, ReferenceType.REVIEW.ToString());
            int currentImageCount = existingImageCount - (request.DeletedImageUrls?.Count ?? 0);
            int newImageCount = request.NewImages?.Count ?? 0;

            if (currentImageCount + newImageCount > 3)
                throw new Exception("Bạn chỉ có thể tải lên tối đa 3 hình ảnh cho mỗi đánh giá");

            if (request.NewImages != null && request.NewImages.Any())
            {
                var mediaList = await _unitOfWork.Media.GetByUrlsAsync(request.NewImages);
                foreach (var media in mediaList)
                {
                    if (media.UploaderId != userId || media.TargetId != null || media.TargetType != null)
                        throw new Exception("Ảnh không hợp lệ");
                    media.TargetId = review.Id;
                    media.TargetType = ReferenceType.REVIEW.ToString();
                    _unitOfWork.Media.Update(media);
                }
            }

            _unitOfWork.Reviews.Update(review);
            return await _unitOfWork.SaveChangesAsync();
        }

        public async Task<int> ValidateCheckinAsync(int userId, int checkInId, CheckinRequest request)
        {
            var member = await _unitOfWork.MembersProfile.GetByUserIdAsync(userId);
            if (member == null)
                throw new Exception("Không tìm thấy hồ sơ thành viên");

            var venue = await _unitOfWork.VenueLocations.GetActiveByIdAsync(request.VenueLocationId);
            if (venue == null)
                throw new Exception("Không tìm thấy địa điểm");

            if (!venue.Latitude.HasValue || !venue.Longitude.HasValue)
                throw new Exception("Địa điểm không có tọa độ hợp lệ");

            var couple = await _unitOfWork.CoupleProfiles.GetActiveCoupleByMemberIdAsync(member.Id);
            int? coupleProfileId = couple?.id;

            // Check if review already exists
            var hasReview = await _unitOfWork.Reviews.HasMemberReviewedVenueAsync(member.Id, request.VenueLocationId, coupleProfileId);
            if (hasReview)
                throw new Exception("Bạn đã đánh giá địa điểm này rồi");

            var checkIn = await _unitOfWork.CheckInHistories.GetByIdAsync(checkInId);
            if (checkIn == null || checkIn.MemberId != member.Id || checkIn.VenueId != request.VenueLocationId)
                throw new Exception("Không tìm thấy lịch sử check-in hợp lệ");

            if (checkIn.IsValid == null)
                throw new Exception("Lịch sử check-in chưa được xác thực, vui lòng đợi hệ thống xác thực tự động hoặc liên hệ hỗ trợ");

            var now = DateTime.UtcNow;
            var minutes = (now - checkIn.CreatedAt.Value).TotalMinutes;
            if (minutes < 10)
                throw new Exception("Vui lòng đợi đủ 10 phút kể từ khi check-in");

            if (checkIn.IsValid == null)
                throw new Exception("Chưa đến bước xác thực (vui lòng đợi thông báo)");

            if (checkIn.IsValid != false)
                throw new Exception("Trạng thái check-in không hợp lệ để xác thực");

            var distance = GeoCalculator.CalculateDistance(
                request.Latitude,
                request.Longitude,
                venue.Latitude.Value,
                venue.Longitude.Value
            );

            if (distance > 0.1)
                throw new Exception("Bạn đang ở quá xa địa điểm để có thể xác thực check-in");

            checkIn.IsValid = true;

            _unitOfWork.CheckInHistories.Update(checkIn);
            return await _unitOfWork.SaveChangesAsync();
        }

        public async Task<PagedResult<MyReviewResponse>> GetMyReviewsAsync(int userId, GetMyReviewRequest request)
        {
            var member = await _unitOfWork.MembersProfile.GetByUserIdAsync(userId);
            if (member == null)
                throw new Exception("Không tìm thấy hồ sơ thành viên");

            request.PageNumber = request.PageNumber <= 0 ? 1 : request.PageNumber;
            request.PageSize = request.PageSize <= 0 ? 10 : request.PageSize;

            var keyword = request.Keyword?.Trim().ToLower();

            var (reviews, totalCoun) = await _unitOfWork.Reviews.GetPagedAsync(
                request.PageNumber,
                request.PageSize,
                r => r.MemberId == member.Id &&
                     r.IsDeleted == false &&
                     (request.VenueId == null || r.VenueId == request.VenueId) &&
                     (string.IsNullOrWhiteSpace(keyword) ||
                      r.Content.ToLower().Contains(keyword) ||
                      r.Venue.Name.ToLower().Contains(keyword)),
                r => request.SortDescending
                    ? r.OrderByDescending(r => r.CreatedAt).ThenByDescending(r => r.Id)
                    : r.OrderBy(r => r.CreatedAt).ThenBy(r => r.Id),
                r => r.Include(r => r.Venue)
                      .Include(r => r.ReviewReply)
                      .Include(r => r.Member)
                        .ThenInclude(m => m.User)
            );

            var reviewIds = reviews.Select(r => r.Id).ToList();

            var mediaLookup = await _unitOfWork.Media.GetByListTargetIdsAsync(
                reviewIds,
                ReferenceType.REVIEW.ToString()
            );

            var myLikedReviews = await _unitOfWork.ReviewLikes.GetAsync(
                rl => rl.MemberId == member.Id && reviewIds.Contains(rl.ReviewId.Value)
            );

            var likedReviewIds = myLikedReviews
                .Select(x => x.ReviewId)
                .ToHashSet();

            var response = _mapper.Map<List<MyReviewResponse>>(reviews);

            foreach (var item in response)
            {
                var reviewEntity = reviews.FirstOrDefault(r => r.Id == item.Id);

                item.ImageUrls = mediaLookup
                    .Where(m => m.TargetId == item.Id && !string.IsNullOrWhiteSpace(m.Url))
                    .Select(m => m.Url!)
                    .ToList();

                if (reviewEntity?.ReviewReply != null)
                {
                    if (item.ReviewReply != null)
                    {
                        item.ReviewReply.VenueId = reviewEntity.VenueId;
                        item.ReviewReply.VenueName = reviewEntity.Venue.Name;
                        item.ReviewReply.VenueCoverImage = DeserializeImages(reviewEntity.Venue.CoverImage);
                    }
                }

                if (reviewEntity.Member != null)
                {
                    var acccessories = await _accessoryService.GetEquippedAccessoryForMemberAsync(reviewEntity.Member.Id);

                    if (item.Member != null)
                    {
                        item.Member = new ReviewMemberInfo
                        {
                            Id = reviewEntity.Member.Id,
                            UserId = reviewEntity.Member.UserId,
                            FullName = reviewEntity.Member.FullName,
                            Gender = reviewEntity.Member.Gender,
                            Bio = reviewEntity.Member.Bio,
                            DisplayName = reviewEntity.Member.User?.DisplayName,
                            AvatarUrl = reviewEntity.Member.User?.AvatarUrl,
                            Email = reviewEntity.Member.User?.Email,
                            EquippedAccessories = acccessories ?? new List<EquippedAccessoryBriefResponse>()
                        };
                    }
                }

                item.HasReply = item.ReviewReply != null;
                item.IsOwner = true;
                item.IsLikedByMe = likedReviewIds.Contains(item.Id);
            }

            return new PagedResult<MyReviewResponse>
            {
                Items = response,
                TotalCount = totalCoun,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize
            };
        }

        public async Task<PagedResult<ReviewResponse>> GetFlaggedReviewsAsync(int pageNumber, int pageSize)
        {
            var (reviews, totalCount) = await _unitOfWork.Reviews.GetPagedAsync(
                pageNumber,
                pageSize,
                r => r.IsDeleted == false && r.Status == ReviewStatus.FLAGGED.ToString(),
                r => r.OrderByDescending(r => r.CreatedAt).ThenByDescending(r => r.Id),
                r => r.Include(r => r.Venue)
                      .Include(r => r.Member)
                        .ThenInclude(m => m.User)
            );

            var reviewIds = reviews.Select(r => r.Id).ToList();

            var mediaLookup = await _unitOfWork.Media.GetByListTargetIdsAsync(
                reviewIds,
                ReferenceType.REVIEW.ToString()
            );

            var response = _mapper.Map<List<ReviewResponse>>(reviews);

            foreach (var item in response)
            {
                item.ImageUrls = mediaLookup
                    .Where(m => m.TargetId == item.Id && !string.IsNullOrWhiteSpace(m.Url))
                    .Select(m => m.Url!)
                    .ToList();
            }

            return new PagedResult<ReviewResponse>
            {
                Items = response,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }

        public async Task<int> ModerateReviewAsync(int reviewId, ModerationRequest request)
        {
            var review = await _unitOfWork.Reviews.GetByIdAsync(reviewId);
            if (review == null || review.IsDeleted == true)
                throw new Exception("Review không tồn tại");

            if (review.Status != ReviewStatus.FLAGGED.ToString())
                throw new Exception("Chỉ có thể duyệt review đang ở trạng thái FLAGGED");

            switch (request.Action)
            {
                case ModerationRequestAction.PUBLISH:
                    review.Status = ReviewStatus.PUBLISHED.ToString();
                    break;
                case ModerationRequestAction.CANCEL:
                    review.Status = ReviewStatus.CANCELLED.ToString();
                    break;
                default:
                    throw new Exception("Action không hợp lệ. Chỉ hỗ trợ PUBLISH hoặc CANCEL");
            }

            review.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.Reviews.Update(review);
            await _unitOfWork.SaveChangesAsync();
            return review.Id;
        }
    }
}
