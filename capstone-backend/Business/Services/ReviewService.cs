using AutoMapper;
using capstone_backend.Business.Common;
using capstone_backend.Business.DTOs.Review;
using capstone_backend.Business.Interfaces;
using capstone_backend.Business.Jobs.Review;
using capstone_backend.Data.Entities;
using capstone_backend.Data.Enums;
using capstone_backend.Extensions.Common;
using Hangfire;
using Microsoft.EntityFrameworkCore;

namespace capstone_backend.Business.Services
{
    public class ReviewService : IReviewService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly S3StorageService _s3Service;

        public ReviewService(IUnitOfWork unitOfWork, IMapper mapper, S3StorageService s3Service)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _s3Service = s3Service;
        }

        public async Task<int> CheckinAsync(int userId, CheckinRequest request)
        {
            var member = await _unitOfWork.MembersProfile.GetByUserIdAsync(userId);
            if (member == null)
                throw new Exception("Không tìm thấy hồ sơ thành viên");
                
            var venue = await _unitOfWork.VenueLocations.GetByIdAsync(request.VenueLocationId);
            if (venue == null)
                throw new Exception("Không tìm thấy địa điểm");

            if (!venue.Latitude.HasValue || !venue.Longitude.HasValue)
                throw new Exception("Địa điểm không có tọa độ hợp lệ");

            // Check if review already exists
            var hasReview = await _unitOfWork.Reviews.HasMemberReviewedVenueAsync(member.Id, request.VenueLocationId);
            if (hasReview)
                throw new Exception("Bạn đã đánh giá địa điểm này rồi");

            var lastCheckin = await _unitOfWork.CheckInHistories.GetLatestByMemberIdAndVenueIdAsync(member.Id, request.VenueLocationId);

            if (lastCheckin != null)
            {
                var timeSinceLastCheckin = DateTime.UtcNow - lastCheckin.CreatedAt;
                if (timeSinceLastCheckin.Value.TotalMinutes < 10)
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
                IsValid = false
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
            var member = await _unitOfWork.MembersProfile.GetByUserIdAsync(userId);
            if (member == null)
                throw new Exception("Không tìm thấy hồ sơ thành viên");

            var venue = await _unitOfWork.VenueLocations.GetByIdAsync(request.VenueLocationId);
            if (venue == null)
                throw new Exception("Không tìm thấy địa điểm");

            // Check if review already exists
            var hasReview = await _unitOfWork.Reviews.HasMemberReviewedVenueAsync(member.Id, request.VenueLocationId);
            if (hasReview)
                throw new Exception("Bạn đã đánh giá địa điểm này rồi");

            var checkIn = await _unitOfWork.CheckInHistories.GetByIdAsync(request.CheckInId);
            if (checkIn == null || checkIn.MemberId != member.Id || checkIn.VenueId != request.VenueLocationId)
                throw new Exception("Không tìm thấy lịch sử check-in hợp lệ");

            if (checkIn.IsValid != true)
                throw new Exception("Lịch sử check-in chưa được xác thực, không thể đánh giá địa điểm");          

            var review = _mapper.Map<Review>(request);
            review.MemberId = member.Id;
            review.VenueId = request.VenueLocationId;
            review.Status = ReviewStatus.PENDING.ToString();

            checkIn.IsValid = false;

            _unitOfWork.CheckInHistories.Update(checkIn);
            await _unitOfWork.Reviews.AddAsync(review);
            await _unitOfWork.SaveChangesAsync();

            // Upload s3
            if (request.Images != null && request.Images.Any())
            {
                foreach (var imageFile in request.Images)
                {
                    var imageUrl = await _s3Service.UploadFileAsync(imageFile, userId, S3Keys.REVIEW);

                    await _unitOfWork.Media.AddAsync(new Media
                    {
                        Url = imageUrl,
                        UploaderId = userId,
                        MediaType = MediaType.IMAGE.ToString(),
                        TargetId = review.Id,
                        TargetType = ReferenceType.REVIEW.ToString()
                    });
                }
            }

            return await _unitOfWork.SaveChangesAsync();
        }

        public async Task<ReviewLikeResponse> ToggleLikeReviewAsync(int userId, int reviewId)
        {
            // Use transaction
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
                    // LIKE
                    var newLike = new ReviewLike 
                    { 
                        ReviewId = reviewId, 
                        MemberId = member.Id 
                    };

                    try
                    {
                        await _unitOfWork.ReviewLikes.AddAsync(newLike);
                        await _unitOfWork.SaveChangesAsync(); // Save để chắc chắn insert thành công
                    }
                    catch (DbUpdateException)
                    {
                        throw new Exception("Bạn đã like đánh giá này rồi (Concurreny check)");
                    }
                    isLiked = true;
                }

                var realCount = await _unitOfWork.ReviewLikes.CountAsync(x => x.ReviewId == reviewId);

                review.LikeCount = realCount;
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

            if (currentImageCount + newImageCount > 5)
                throw new Exception("Bạn chỉ có thể tải lên tối đa 5 hình ảnh cho mỗi đánh giá");

            if (request.NewImages != null && request.NewImages.Any())
            {
                foreach (var imageFile in request.NewImages)
                {
                    string imageUrl = await _s3Service.UploadFileAsync(imageFile, userId, S3Keys.REVIEW);

                    var newImage = new Media
                    {
                        Url = imageUrl,
                        UploaderId = userId,
                        MediaType = MediaType.IMAGE.ToString(),
                        TargetId = review.Id,
                        TargetType = ReferenceType.REVIEW.ToString()
                    };

                    await _unitOfWork.Media.AddAsync(newImage);
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

            var venue = await _unitOfWork.VenueLocations.GetByIdAsync(request.VenueLocationId);
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
    }
}
