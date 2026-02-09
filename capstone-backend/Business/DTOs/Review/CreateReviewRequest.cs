namespace capstone_backend.Business.DTOs.Review
{
    public class CreateReviewRequest
    {
        public int VenueLocationId { get; set; }
        public int CheckInId { get; set; }
        /// <example>Thật tuyệt vời!</example>
        public string? Content { get; set; } = null!;
        /// <example>5</example>
        public int Rating { get; set; }
        /// <example>false</example>
        public bool IsAnonymous { get; set; }
        public List<IFormFile>? Images { get; set; }
    }
}
