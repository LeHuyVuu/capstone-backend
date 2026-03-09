using capstone_backend.Data.Entities;
using System.ComponentModel.DataAnnotations;

namespace capstone_backend.Business.DTOs.Post
{
    public class UpdatePostRequest
    {
        /// <example>Chỉnh lại nội dung cho hay hơn một chút</example>
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

        /// <example>Hà Nội</example>
        public string? LocationName { get; set; }

        /// <example>PRIVATE</example>
        public string Visibility { get; set; }

        /// <example>
        /// ["#edited", "#newvibes"]
        /// </example>
        public List<string> HashTags { get; set; } = new();

        /// <example>
        /// ["deep-talk", "memories"]
        /// </example>
        public List<string> Topic { get; set; } = new();
    }
}
