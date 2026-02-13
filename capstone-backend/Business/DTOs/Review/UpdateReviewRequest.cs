using System.ComponentModel.DataAnnotations;

namespace capstone_backend.Business.DTOs.Review
{
    public class UpdateReviewRequest
    {
        public int VenueLocationId { get; set; }
        [Range(1, 5, ErrorMessage = "Điểm đánh giá phải nằm trong khoảng [1 - 5]")]
        public int Rating { get; set; }
        public string? Content { get; set; }
        public bool IsAnonymous { get; set; }
        public List<string>? DeletedImageUrls { get; set; }
        public List<string>? NewImages { get; set; }
    }
}
