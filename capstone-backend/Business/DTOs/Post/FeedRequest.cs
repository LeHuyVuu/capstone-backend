namespace capstone_backend.Business.DTOs.Post
{
    public class FeedRequest
    {
        /// <example>10</example>
        public int PageSize { get; set; } = 20;
        public long? Cursor { get; set; }

        // Optional
        public decimal? CurrentLatitude { get; set; }
        public decimal? CurrentLongitude { get; set; }
    }
}
