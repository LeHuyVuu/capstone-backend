using capstone_backend.Data.Enums;

namespace capstone_backend.Business.DTOs.Voucher
{
    public class GetPendingVouchersRequest
    {
        /// <example>1</example>
        public int PageNumber { get; set; } = 1;

        /// <example>10</example>
        public int PageSize { get; set; } = 10;

        public string? Keyword { get; set; }

        public int? VenueOwnerId { get; set; }

        /// <summary>
        /// SortBy:
        /// - createdAt
        /// - updatedAt
        /// </summary>
        /// <example>createdAt</example>
        public string? SortBy { get; set; } = "createdAt";

        /// <summary>
        /// OrderBy:
        /// - asc
        /// - desc
        /// </summary>
        /// <example>desc</example>
        public string? OrderBy { get; set; } = "desc";
    }
}
