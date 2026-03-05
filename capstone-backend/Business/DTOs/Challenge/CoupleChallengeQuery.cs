using capstone_backend.Data.Enums;

namespace capstone_backend.Business.DTOs.Challenge
{
    public class CoupleChallengeQuery
    {
        /// <summary>
        /// Số trang (mặc định 1)
        /// </summary>
        /// <example>1</example>
        public int PageNumber { get; set; } = 1;

        /// <summary>
        /// Số item mỗi trang (mặc định 10)
        /// </summary>
        /// <example>10</example>
        public int PageSize { get; set; } = 10;

        // Optional
        /// <summary>
        /// Lọc theo trạng thái challenge của couple
        /// - IN_PROGRESS: Thử thách đang thực hiện
        /// - COMPLETED: Thử thách đã hoàn thành
        /// - IN_COMPLETED: Thử thách chưa hoàn thành
        /// - Nếu truyền gì, sẽ trả về tất cả thử thách bất kể trạng thái
        /// </summary>
        public CoupleProfileChallengeStatus? Status { get; set; }     // IN_PROGRESS | COMPLETED | IN_COMPLETED

        /// <summary>
        /// Tìm kiếm theo tiêu đề (title) thử thách
        /// </summary>
        public string? Q { get; set; }          // search title

        /// <summary>
        /// Lọc theo ngày tham gia (joinedAt >= from) - định dạng ISO 8601 (UTC)
        /// </summary>
        public DateTime? From { get; set; }     // joinedAt >= from (UTC)

        /// <summary>
        /// Lọc theo ngày tham gia (joinedAt &lt;= to) - định dạng ISO 8601 (UTC)
        /// </summary>
        public DateTime? To { get; set; }       // joinedAt <= to (UTC)

        /// <summary>
        /// Sắp xếp kết quả
        /// - updatedAtAsc: Sắp xếp theo ngày cập nhật tăng dần
        /// - updatedAtDesc: Sắp xếp theo ngày cập nhật giảm dần
        /// - joinedAtDesc: Sắp xếp theo ngày tham gia giảm dần
        /// - joinedAtAsc: Sắp xếp theo ngày tham gia tăng dần
        /// </summary>
        public string? Sort { get; set; }       // updatedAtDesc | joinedAtDesc | joinedAtAsc
    }
}
