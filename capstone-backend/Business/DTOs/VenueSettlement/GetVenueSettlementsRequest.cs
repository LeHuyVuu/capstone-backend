using capstone_backend.Data.Enums;

namespace capstone_backend.Business.DTOs.VenueSettlement
{
    public class GetVenueSettlementsRequest
    {
        /// <example>1</example>
        public int PageNumber { get; set; } = 1;
        /// <example>10</example>
        public int PageSize { get; set; } = 10;

        /// <example>PENDING</example>
        public VenueSettlementStatus Status { get; set; } = VenueSettlementStatus.PENDING;
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }

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
