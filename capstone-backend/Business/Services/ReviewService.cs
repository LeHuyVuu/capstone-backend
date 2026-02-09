using capstone_backend.Business.DTOs.Review;
using capstone_backend.Business.Interfaces;
using capstone_backend.Business.Jobs.Review;
using capstone_backend.Data.Entities;
using capstone_backend.Extensions.Common;
using Hangfire;

namespace capstone_backend.Business.Services
{
    public class ReviewService : IReviewService
    {
        private readonly IUnitOfWork _unitOfWork;

        public ReviewService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
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
    }
}
