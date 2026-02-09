namespace capstone_backend.Business.DTOs.Review
{
    public class CreateReviewRequest
    {
        public int VenueLocationId { get; set; }
        public int CheckInId { get; set; }
        public string Content { get; set; } = null!;
        public int Rating { get; set; }
        public bool IsAnonymous { get; set; }
    }
}
