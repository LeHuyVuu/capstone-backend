namespace capstone_backend.Business.DTOs.Voucher
{
    public class GetVoucherRedemptionsRequest
    {
        /// <example>1</example>
        public int PageNumber { get; set; } = 1;

        /// <example>10</example>
        public int PageSize { get; set; } = 10;

        public string? Keyword { get; set; }

        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }

        /// <summary>
        /// SortBy:
        /// - usedAt
        /// </summary>
        /// <example>usedAt</example>
        public string? SortBy { get; set; } = "usedAt";

        /// <summary>
        /// OrderBy:
        /// - asc
        /// - desc
        /// </summary>
        /// <example>desc</example>
        public string? OrderBy { get; set; } = "desc";
    }
}
