using capstone_backend.Data.Enums;

namespace capstone_backend.Business.DTOs.Voucher
{
    public class GetVoucherItemsRequest
    {
        /// <example>1</example>
        public int PageNumber { get; set; } = 1;

        /// <example>10</example>
        public int PageSize { get; set; } = 10;

        /// <summary>
        /// Status:
        /// - AVAILABLE
        /// - ACQUIRED
        /// - USED
        /// - EXPIRED
        /// - ENDED
        /// </summary>
        public VoucherItemStatus? Status { get; set; }

        public string? Code { get; set; }

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
