using Amazon.S3.Model.Internal.MarshallTransformations;
using AutoMapper;
using capstone_backend.Business.Common;
using capstone_backend.Business.DTOs.Accessory;
using capstone_backend.Business.DTOs.Common;
using capstone_backend.Business.DTOs.CoupleMoodType;
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
using Microsoft.IdentityModel.Tokens;
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
        private readonly ISystemConfigService _systemConfigService;

        public ReviewService(IUnitOfWork unitOfWork, IMapper mapper, S3StorageService s3Service, IModerationService moderationService, IAccessoryService accessoryService, ISystemConfigService systemConfigService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _s3Service = s3Service;
            _moderationService = moderationService;
            _accessoryService = accessoryService;
            _systemConfigService = systemConfigService;
        }

        public async Task<int> CheckinAsync(int userId, CheckinRequest request)
        {
            var now = DateTime.UtcNow;

            var member = await _unitOfWork.MembersProfile.GetByUserIdAsync(userId);
            if (member == null)
                throw new Exception("Không tìm thấy hồ sơ thành viên");

            var couple = await _unitOfWork.CoupleProfiles.GetActiveCoupleByMemberIdAsync(member.Id);
            if (couple == null)
                throw new Exception("Bạn không có hồ sơ cặp đôi");

            var venue = await _unitOfWork.VenueLocations.GetByIdWithDetailsAsync(request.VenueLocationId);
            if (venue == null)
                throw new Exception("Không tìm thấy địa điểm");

            if (!venue.Latitude.HasValue || !venue.Longitude.HasValue)
                throw new Exception("Địa điểm không có tọa độ hợp lệ");

            // Check opening hours
            var nowVn = TimezoneUtil.ToVietNamTime(now);
            var today = nowVn.DayOfWeek == DayOfWeek.Sunday
                    ? 8
                    : (int)nowVn.DayOfWeek + 1;

            var currentTime = nowVn.TimeOfDay;

            var openingHours = venue.VenueOpeningHours;
            if (openingHours == null || !openingHours.Any())
                throw new Exception("Địa điểm hôm nay không mở cửa");

            var isOpen = openingHours.Any(oh =>
            {

                var open = oh.OpenTime;
                var close = oh.CloseTime;

                if (close < open)
                {
                    return currentTime >= open || currentTime <= close;
                }

                return currentTime >= open && currentTime <= close;
            });

            if (!isOpen)
                throw new Exception("Địa điểm hiện tại đang đóng cửa");

            // Check if review already exists
            var hasReview = await _unitOfWork.Reviews.HasMemberReviewedVenueAsync(member.Id, request.VenueLocationId);
            if (hasReview)
                throw new Exception("Bạn đã đánh giá địa điểm này rồi");

            var delaySeconds = await GetCheckinReviewDelaySecondsAsync();
            var delayMinutes = ToDisplayMinutes(delaySeconds);
            var lastCheckin = await _unitOfWork.CheckInHistories.GetLatestByMemberIdAndVenueIdAsync(
                    member.Id,
                    request.VenueLocationId,
                    delaySeconds);

            if (lastCheckin != null && lastCheckin.IsValid == null)
            {
                if (!lastCheckin.CreatedAt.HasValue)
                    throw new InvalidOperationException("Bạn vừa check-in rồi, hãy đợi thông báo xác thực nhé!");

                var elapsedSeconds = (now - lastCheckin.CreatedAt.Value).TotalSeconds;
                if (elapsedSeconds + 1 < delaySeconds)
                {
                    var remainingSeconds = Math.Max(1, (int)Math.Ceiling(delaySeconds - elapsedSeconds));
                    var remainingMinutes = Math.Max(1, (int)Math.Ceiling(remainingSeconds / 60.0));

                    throw new Exception($"Vui lòng đợi thêm khoảng {remainingMinutes} phút ({remainingSeconds} giây) để nhận thông báo xác thực");
                }
            }

            // Validate radius
            var radiusM = await _systemConfigService.GetIntValueAsync(SystemConfigKeys.CHECKIN_RADIUS_M.ToString());

            var distance = GeoCalculator.CalculateDistance(
                request.Latitude,
                request.Longitude,
                venue.Latitude.Value,
                venue.Longitude.Value
            ) * 1000;

            if (distance > radiusM)
                throw new Exception(
                    $"Bạn đang cách địa điểm {distance:F0}m. " +
                    $"Chỉ được check-in trong phạm vi {radiusM}m."
                );

            // Save check in
            var checkIn = new CheckInHistory
            {
                MemberId = member.Id,
                VenueId = request.VenueLocationId,
                Latitude = request.Latitude,
                Longitude = request.Longitude,
                CreatedAt = now,
                IsValid = null, // Invalid until validated
            };

            await _unitOfWork.CheckInHistories.AddAsync(checkIn);
            await _unitOfWork.SaveChangesAsync();

            // Notify after delaySeconds to validate check-in
            BackgroundJob.Schedule<IReviewWorker>(
                worker => worker.SendReviewNotificationAsync(checkIn.Id),
                TimeSpan.FromSeconds(delaySeconds)
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
            var now = DateTime.UtcNow;

            if (request.Images != null && request.Images.Count > 3)
                throw new Exception("Bạn chỉ có thể tải lên tối đa 3 hình ảnh cho mỗi đánh giá");

            var member = await _unitOfWork.MembersProfile.GetByUserIdAsync(userId);
            if (member == null)
                throw new Exception("Không tìm thấy hồ sơ thành viên");

            var couple = await _unitOfWork.CoupleProfiles.GetActiveCoupleByMemberIdAsync(member.Id);
            if (couple == null)
                throw new Exception("Bạn không có hồ sơ cặp đôi");

            // Check if couple have mood?
            if (request.IsMatched)
            {
                // matched → phải có current mood
                if (couple.CoupleMoodTypeId == null)
                    throw new Exception("Bạn cần cập nhật mood hiện tại của cả hai trước khi đánh giá");
            }
            else
            {
                // not matched → phải chọn mood
                if (!request.CoupleMoodTypeId.HasValue)
                    throw new Exception("Bạn cần chọn mood khi hai bạn không match với địa điểm");
            }

            var venue = await _unitOfWork.VenueLocations.GetByIdWithDetailsAsync(request.VenueLocationId);
            if (venue == null)
                throw new Exception("Không tìm thấy địa điểm");

            // Check opening hours
            var nowVn = TimezoneUtil.ToVietNamTime(now);
            var today = nowVn.DayOfWeek == DayOfWeek.Sunday
                    ? 8
                    : (int)nowVn.DayOfWeek + 1;

            var currentTime = nowVn.TimeOfDay;

            var openingHours = venue.VenueOpeningHours;
            if (openingHours == null || !openingHours.Any())
                throw new Exception("Địa điểm hôm nay không mở cửa");

            var isOpen = openingHours.Any(oh =>
            {

                var open = oh.OpenTime;
                var close = oh.CloseTime;

                if (close < open)
                {
                    return currentTime >= open || currentTime <= close;
                }

                return currentTime >= open && currentTime <= close;
            });

            if (!isOpen)
                throw new Exception("Địa điểm hiện tại đang đóng cửa");

            // Check if review already exists
            var hasPublishedReview = await _unitOfWork.Reviews.HasMemberReviewedVenueAsync(member.Id, request.VenueLocationId);
            if (hasPublishedReview)
                throw new Exception("Bạn đã đánh giá địa điểm này rồi");

            var existingFlagged = await _unitOfWork.Reviews.GetFirstAsync(r =>
                r.MemberId == member.Id &&
                r.VenueId == request.VenueLocationId &&
                r.IsDeleted == false &&
                r.Status == ReviewStatus.FLAGGED.ToString());

            if (existingFlagged != null)
                throw new Exception("Đánh giá của bạn đang được admin xem xét, chưa thể gửi đánh giá mới");

            var checkIn = await _unitOfWork.CheckInHistories.GetByIdAsync(request.CheckInId);
            if (checkIn == null || checkIn.MemberId != member.Id || checkIn.VenueId != request.VenueLocationId)
                throw new Exception("Không tìm thấy lịch sử check-in hợp lệ");

            if (checkIn.IsValid != true)
                throw new Exception("Lịch sử check-in chưa được xác thực, không thể đánh giá địa điểm");

            // Moderation
            var toCheck = new List<string> { request.Content };
            if (request.Images != null && request.Images.Any())
                toCheck.AddRange(request.Images);
            var moderationResults = toCheck.Any()
                    ? await _moderationService.CheckContentByAIService(toCheck)
                    : new List<ModerationResultDto>();

            if (moderationResults.Any(r => r.Action == ModerationAction.BLOCK))
                throw new Exception("Nội dung của bạn đã bị hệ thống chặn vì vi phạm tiêu chuẩn cộng đồng");

            Review? review = null;
            var hasImage = false;

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                // Soft delete un-published review if exist to prevent duplicate review when user submit multiple times before the first review is published
                var existingReplaceable = await _unitOfWork.Reviews.GetFirstAsync(r =>
                    r.MemberId == member.Id &&
                    r.VenueId == request.VenueLocationId &&
                    r.IsDeleted == false &&
                    (r.Status == ReviewStatus.PENDING.ToString() || r.Status == ReviewStatus.CANCELLED.ToString()));

                if (existingReplaceable != null)
                {
                    existingReplaceable.IsDeleted = true;
                    existingReplaceable.UpdatedAt = DateTime.UtcNow;
                    _unitOfWork.Reviews.Update(existingReplaceable);

                    var oldMedia = await _unitOfWork.Media.GetByTargetIdAndTypeAsync(existingReplaceable.Id, ReferenceType.REVIEW.ToString());
                    foreach (var media in oldMedia)
                    {
                        media.IsDeleted = true;
                        media.UpdatedAt = DateTime.UtcNow;
                        _unitOfWork.Media.Update(media);
                    }
                }

                review = _mapper.Map<Review>(request);
                review.MemberId = member.Id;
                review.VenueId = request.VenueLocationId;
                review.Status = ReviewStatus.PENDING.ToString();
                review.IsAnonymous = request.IsAnonymous;
                review.IsMatched = request.IsMatched;
                review.CoupleMoodSnapshot = await BuildCoupleMoodSnapshotAsync(member.Id, request.CoupleMoodTypeId, request.IsMatched);

                checkIn.IsValid = false;
                _unitOfWork.CheckInHistories.Update(checkIn);

                await _unitOfWork.Reviews.AddAsync(review);
                await _unitOfWork.SaveChangesAsync();

                // Handle images
                if (request.Images != null && request.Images.Any())
                {
                    var requestedUrls = request.Images.Distinct().ToList();
                    var mediaList = (await _unitOfWork.Media.GetByUrlsAsync(requestedUrls)).ToList();

                    if (mediaList.Count != requestedUrls.Count)
                        throw new Exception("Ảnh không hợp lệ");

                    foreach (var media in mediaList)
                    {
                        if (media.UploaderId != userId || media.TargetId != null || media.TargetType != null)
                            throw new Exception("Ảnh không hợp lệ");

                        media.TargetId = review.Id;
                        media.TargetType = ReferenceType.REVIEW.ToString();
                        _unitOfWork.Media.Update(media);
                    }

                    hasImage = true;
                    await _unitOfWork.SaveChangesAsync();
                }

                await _unitOfWork.CommitTransactionAsync();
            }
            catch (DbUpdateException)
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw new Exception("Lỗi cơ sở dữ liệu khi gửi đánh giá. Vui lòng thử lại.");
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }

            BackgroundJob.Enqueue<IModerationWorker>(j => j.ProcessReviewModerationAndChallengeAsync(review.Id, moderationResults, userId, review.VenueId, hasImage));

            return review.Id;
        }

        private async Task<string?> BuildCoupleMoodSnapshotAsync(int memberId, int? coupleMoodTypeId, bool? isMatched)
        {
            var couple = await _unitOfWork.Context.CoupleProfiles
                .AsNoTracking()
                .Include(c => c.CouplePersonalityType)
                .Include(c => c.CoupleMoodType)
                .FirstOrDefaultAsync(c =>
                    c.IsDeleted != true &&
                    c.Status == CoupleProfileStatus.ACTIVE.ToString() &&
                    (c.MemberId1 == memberId || c.MemberId2 == memberId));

            if (couple == null)
                return null;

            var values = new List<string>();

            if (!string.IsNullOrWhiteSpace(couple.CouplePersonalityType?.Name))
                values.Add(couple.CouplePersonalityType.Name.Trim());

            string? moodName = null;

            if (isMatched == false && coupleMoodTypeId.HasValue)
            {
                var moodType = await _unitOfWork.Context.CoupleMoodTypes
                    .AsNoTracking()
                    .FirstOrDefaultAsync(m => m.IsDeleted != true && m.Id == coupleMoodTypeId);

                if (moodType == null)
                    throw new Exception("Không tìm thấy loại tâm trạng cặp đôi hợp lệ");

                moodName = moodType.Name;
            }
            else
            {
                moodName = couple.CoupleMoodType?.Name;
            }

            if (!string.IsNullOrWhiteSpace(moodName))
                values.Add(moodName.Trim());

            //var moodType = await _unitOfWork.Context.CoupleMoodTypes
            //    .AsNoTracking()
            //    .FirstOrDefaultAsync(m => m.IsDeleted != true && m.Id == coupleMoodTypeId);

            //if (moodType == null)
            //    throw new Exception("Không tìm thấy loại tâm trạng cặp đôi hợp lệ");

            //values.Add(moodType.Name.Trim());

            //if (!string.IsNullOrWhiteSpace(couple.CoupleMoodType?.Name))
            //    values.Add(couple.CoupleMoodType.Name.Trim());

            return values.Any() ? string.Join(",", values) : null;
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

            var couple = await _unitOfWork.CoupleProfiles.GetActiveCoupleByMemberIdAsync(member.Id);
            if (couple == null)
                throw new Exception("Bạn không có hồ sơ cặp đôi");

            if (request.IsMatched.HasValue)
            {
                if (request.IsMatched.Value)
                {
                    // matched → phải có current mood
                    if (couple.CoupleMoodTypeId == null)
                        throw new Exception("Bạn cần cập nhật mood hiện tại của cả hai trước khi đánh giá");
                }
                else
                {
                    // not matched → phải chọn mood
                    if (!request.CoupleMoodTypeId.HasValue)
                        throw new Exception("Bạn cần chọn mood khi hai bạn không match với địa điểm");
                }
            }

            var review = await _unitOfWork.Reviews.GetByIdAndMemberIdAsync(reviewId, member.Id);
            if (review == null)
                throw new Exception("Không tìm thấy đánh giá hợp lệ");

            _mapper.Map(request, review);
            review.CoupleMoodSnapshot = await BuildCoupleMoodSnapshotAsync(member.Id, request.CoupleMoodTypeId, request.IsMatched);

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
            var affected = await _unitOfWork.SaveChangesAsync();

            BackgroundJob.Enqueue<IReviewWorker>(j => j.EvaluateReviewRelevanceAsync(review.Id));
            BackgroundJob.Enqueue<IReviewWorker>(j => j.RecountReviewAsync(review.VenueId));

            return affected;
        }

        public async Task<int> ValidateCheckinAsync(int userId, int checkInId, CheckinRequest request)
        {
            var member = await _unitOfWork.MembersProfile.GetByUserIdAsync(userId);
            if (member == null)
                throw new Exception("Không tìm thấy hồ sơ thành viên");

            var couple = await _unitOfWork.CoupleProfiles.GetActiveCoupleByMemberIdAsync(member.Id);
            if (couple == null)
                throw new Exception("Bạn không có hồ sơ cặp đôi");

            var venue = await _unitOfWork.VenueLocations.GetActiveByIdAsync(request.VenueLocationId);
            if (venue == null)
                throw new Exception("Không tìm thấy địa điểm");

            if (!venue.Latitude.HasValue || !venue.Longitude.HasValue)
                throw new Exception("Địa điểm không có tọa độ hợp lệ");

            // Check if review already exists
            var hasReview = await _unitOfWork.Reviews.HasMemberReviewedVenueAsync(member.Id, request.VenueLocationId);
            if (hasReview)
                throw new Exception("Bạn đã đánh giá địa điểm này rồi");

            var checkIn = await _unitOfWork.CheckInHistories.GetByIdAsync(checkInId);
            if (checkIn == null || checkIn.MemberId != member.Id || checkIn.VenueId != request.VenueLocationId)
                throw new Exception("Không tìm thấy lịch sử check-in hợp lệ");

            if (checkIn.IsValid == true)
                return checkInId; // Đã được xác thực trước đó

            if (checkIn.IsValid == null)
                throw new Exception("Check-in đang chờ hệ thống xử lý");

            if (!checkIn.CreatedAt.HasValue)
                throw new Exception("Không thể xác định thời gian check-in hợp lệ");

            var now = DateTime.UtcNow;
            var delaySeconds = await GetCheckinReviewDelaySecondsAsync();
            var elapsedSeconds = (now - checkIn.CreatedAt.Value).TotalSeconds;

            // Grace 1 giây để tránh lệch clock/queue
            if (elapsedSeconds + 1 < delaySeconds)
            {
                var remainingSeconds = Math.Max(1, (int)Math.Ceiling(delaySeconds - elapsedSeconds));
                var remainingMinutes = Math.Max(1, (int)Math.Ceiling(remainingSeconds / 60.0));

                throw new Exception($"Vui lòng đợi thêm khoảng {remainingMinutes} phút ({remainingSeconds} giây) kể từ khi check-in");
            }

            var radiusM = await _systemConfigService.GetIntValueAsync(SystemConfigKeys.CHECKIN_RADIUS_M.ToString());

            var distance = GeoCalculator.CalculateDistance(
                request.Latitude,
                request.Longitude,
                venue.Latitude.Value,
                venue.Longitude.Value
            ) * 1000;

            if (distance > radiusM)
                throw new Exception(
                    $"Bạn đang cách địa điểm {distance:F0}m. " +
                    $"Chỉ được check-in trong phạm vi {radiusM}m."
                );

            checkIn.IsValid = true;

            _unitOfWork.CheckInHistories.Update(checkIn);
            await _unitOfWork.SaveChangesAsync();
            return checkInId;
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
            var review = await _unitOfWork.Reviews.GetFirstAsync(r => r.Id == reviewId && r.IsDeleted == false, r => r.Include(r => r.Member));
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

            if (review.Status == ReviewStatus.PUBLISHED.ToString())
            {
                BackgroundJob.Enqueue<IReviewWorker>(j => j.EvaluateReviewRelevanceAsync(review.Id));
            }

            BackgroundJob.Enqueue<IModerationWorker>(j => j.NotifyResultModerationAsync(review.Member.UserId, review.Id, ModerationContentType.REVIEW, request.Action));

            return review.Id;
        }

        private async Task<int> GetCheckinReviewDelaySecondsAsync()
        {
            const int defaultDelaySeconds = 600; // 10 phút
            const int minDelaySeconds = 5;
            const int maxDelaySeconds = 3600;

            try
            {
                var delaySeconds = await _systemConfigService.GetIntValueAsync(
                    SystemConfigKeys.CHECKIN_REVIEW_NOTIFICATION_DELAY_SECONDS.ToString());

                if (delaySeconds < minDelaySeconds || delaySeconds > maxDelaySeconds)
                    return defaultDelaySeconds;

                return delaySeconds;
            }
            catch
            {
                return defaultDelaySeconds;
            }
        }

        private static int ToDisplayMinutes(int seconds)
        {
            return Math.Max(1, (int)Math.Ceiling(seconds / 60.0));
        }

        public async Task<List<CoupleMoodTypeResponse>> GetCoupleMoodTypeAsync()
        {
            var coupleMoodTypes = await _unitOfWork.Context.CoupleMoodTypes
                .AsNoTracking()
                .Where(cmt => cmt.IsDeleted != true && cmt.IsActive == true)
                .ToListAsync();

            var response = _mapper.Map<List<CoupleMoodTypeResponse>>(coupleMoodTypes);

            return response;
        }
    }
}
