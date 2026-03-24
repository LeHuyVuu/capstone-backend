using capstone_backend.Data.Enums;

namespace capstone_backend.Business.DTOs.Accessory
{
    public class GetMyAccessoryRequest
    {
        /// <example>1</example>
        public int PageNumber { get; set; } = 1;
        /// <example>10</example>
        public int PageSize { get; set; } = 10;

        public bool EquippedOnly { get; set; } = false;

        public AccessoryType? Type { get; set; }
        public string? Keyword { get; set; }

        /// <summary>
        /// SortBy:
        /// - acquiredAt
        /// - name
        /// </summary>
        /// <example>createdAt</example>
        public string? SortBy { get; set; } = "acquiredAt";

        /// <summary>
        /// OrderBy:
        /// - asc
        /// - desc
        /// </summary>
        /// <example>desc</example>
        public string? OrderBy { get; set; } = "desc";
    }
}
