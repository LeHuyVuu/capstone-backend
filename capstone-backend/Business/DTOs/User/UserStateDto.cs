namespace capstone_backend.Business.DTOs.User
{
    public class UserStateDto
    {
        public bool HasReviewedBefore { get; set; }
        public int? ActiceCheckInId { get; set; }
        public bool CanReview { get; set; } = false;
    }
}
