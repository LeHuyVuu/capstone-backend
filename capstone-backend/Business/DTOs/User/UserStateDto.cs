namespace capstone_backend.Business.DTOs.User
{
    public class UserStateDto
    {
        public bool HasReviewedBefore { get; set; }
        public int? ActiveCheckInId { get; set; }
        public bool CanReview { get; set; } = false;
    }
}
