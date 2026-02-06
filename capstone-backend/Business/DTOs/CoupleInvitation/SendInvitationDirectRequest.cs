namespace capstone_backend.Business.DTOs.CoupleInvitation;

/// <summary>
/// Request DTO để gửi lời mời ghép đôi trực tiếp (Case 2)
/// </summary>
public class SendInvitationDirectRequest
{
    /// <summary>
    /// ID của member nhận lời mời
    /// </summary>
    public int ReceiverMemberId { get; set; }

    /// <summary>
    /// Lời nhắn kèm theo (optional)
    /// </summary>
    public string? Message { get; set; }
}
