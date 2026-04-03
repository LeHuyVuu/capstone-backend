using capstone_backend.Data.Enums;

namespace capstone_backend.Business.DTOs.Moderation
{
    public class ModerationRequest
    {
        public ModerationRequestAction Action { get; set; }
    }
}
