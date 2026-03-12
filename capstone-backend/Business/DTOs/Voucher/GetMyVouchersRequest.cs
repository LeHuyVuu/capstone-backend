using capstone_backend.Data.Enums;

namespace capstone_backend.Business.DTOs.Voucher
{
    public class GetMyVouchersRequest
    {
        /// <example>1</example>
        public int PageNumber { get; set; } = 1;

        /// <example>10</example>
        public int PageSize { get; set; } = 10;

        public string? Keyword { get; set; }

        public int? VoucherId { get; set; }

        /// <summary>
        /// Status:
        /// - ACQUIRED
        /// - USED
        /// - EXPIRED
        /// </summary>
        public VoucherItemStatus? Status { get; set; }

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
