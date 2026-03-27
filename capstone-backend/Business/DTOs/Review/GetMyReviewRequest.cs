namespace capstone_backend.Business.DTOs.Review
{
    public class GetMyReviewRequest
    {
        /// <example>1</example>
        public int PageNumber { get; set; } = 1;

        /// <example>10</example>
        public int PageSize { get; set; } = 10;

        /// <summary>
        /// Lọc theo địa điểm cụ thể
        /// </summary>
        public int? VenueId { get; set; }

        /// <summary>
        /// Từ khóa tìm theo nội dung review hoặc tên địa điểm.
        /// </summary>
        public string? Keyword { get; set; }

        /// <summary>
        /// Sắp xếp mới nhất trước hay cũ nhất trước.
        /// </summary>
        /// <example>true</example>
        public bool SortDescending { get; set; } = true;
    }
}
