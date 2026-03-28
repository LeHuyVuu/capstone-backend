using capstone_backend.Business.DTOs.User;
using capstone_backend.Data.Entities;

namespace capstone_backend.Business.DTOs.Review
{
    public class ReviewReplyResponse
    {
        public int Id { get; set; }
        public int ReviewId { get; set; }
        public int VenueId { get; set; }
        public string? VenueName { get; set; }
        public List<string>? VenueCoverImage { get; set; }
        public string Content { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
