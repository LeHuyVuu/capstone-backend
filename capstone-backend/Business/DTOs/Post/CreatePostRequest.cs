using capstone_backend.Data.Entities;
using System.ComponentModel.DataAnnotations;

namespace capstone_backend.Business.DTOs.Post
{
    public class CreatePostRequest
    {
        /// <example>Một ngày thật đẹp!</example>
        [Required(ErrorMessage = "Thiếu nội dung")]
        public string Content { get; set; }

        /// <example>
        /// [
        ///   {
        ///     "url": "https://couplemood-store.s3.ap-southeast-2.amazonaws.com/images/53/75fd3b34-385e-476d-8b72-db4da0eee484.png",
        ///     "type": "IMAGE"
        ///   }
        /// ]
        /// </example>
        public List<MediaItem> MediaPayload { get; set; } = new();

        /// <example>HCM</example>
        public string? LocationName { get; set; }

        /// <example>PUBLIC</example>
        public string Visibility { get; set; }

        /// <example>
        /// ["#love", "#weekend"]
        /// </example>
        public List<string> HashTags { get; set; } = new();

        /// <example>
        /// ["deep-talk", "experiences"]
        /// </example>
        public List<string> Topic { get; set; } = new();
    }
}
