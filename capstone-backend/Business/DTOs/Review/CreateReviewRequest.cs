using System.ComponentModel.DataAnnotations;

namespace capstone_backend.Business.DTOs.Review
{
    public class CreateReviewRequest
    {
        public int VenueLocationId { get; set; }
        public int CheckInId { get; set; }
        /// <example>Thật tuyệt vời!</example>
        public string? Content { get; set; } = null!;
        /// <example>5</example>
        [Range(1, 5, ErrorMessage = "Điểm đánh giá phải nằm trong khoảng [1 - 5]")]
        public int Rating { get; set; }
        /// <example>false</example>
        public bool IsAnonymous { get; set; }
        public List<string>? Images { get; set; }
    }
}
