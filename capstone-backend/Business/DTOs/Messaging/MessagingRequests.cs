using System.ComponentModel.DataAnnotations;

namespace capstone_backend.Business.DTOs.Messaging;

/// <summary>
/// DTO for creating a new conversation
/// </summary>
public class CreateConversationRequest
{
    [Required(ErrorMessage = "Loại cuộc trò chuyện là bắt buộc")]
    public string Type { get; set; } = string.Empty;
    
    public string? Name { get; set; }
    
    [Required(ErrorMessage = "Cần ít nhất một thành viên")]
    [MinLength(1, ErrorMessage = "Cần ít nhất một thành viên")]
    public List<int> MemberIds { get; set; } = new();
}

/// <summary>
/// DTO for sending a message
/// </summary>
public class SendMessageRequest
{
    [Required(ErrorMessage = "Conversation ID là bắt buộc")]
    public int ConversationId { get; set; }
    
    public string? Content { get; set; }
    
    public string MessageType { get; set; } = "TEXT"; // TEXT, IMAGE, FILE, VIDEO, AUDIO
    
    public int? ReferenceId { get; set; }
    
    public string? ReferenceType { get; set; }
    
    /// <summary>
    /// File URL (sau khi upload qua /api/messaging/upload-attachment)
    /// </summary>
    public string? FileUrl { get; set; }
    
    /// <summary>
    /// File name gốc
    /// </summary>
    public string? FileName { get; set; }
    
    /// <summary>
    /// File size (bytes)
    /// </summary>
    public long? FileSize { get; set; }
    
    public string? Metadata { get; set; }
}

/// <summary>
/// DTO for marking message as read
/// </summary>
public class MarkAsReadRequest
{
    [Required(ErrorMessage = "Conversation ID là bắt buộc")]
    public int ConversationId { get; set; }
    
    [Required(ErrorMessage = "Message ID là bắt buộc")]
    public int MessageId { get; set; }
}

/// <summary>
/// DTO for typing indicator
/// </summary>
public class TypingIndicatorRequest
{
    [Required(ErrorMessage = "Conversation ID là bắt buộc")]
    public int ConversationId { get; set; }
    
    [Required(ErrorMessage = "Trạng thái đang nhập là bắt buộc")]
    public bool IsTyping { get; set; }
}

/// <summary>
/// DTO for adding members to group
/// </summary>
public class AddMembersRequest
{
    [Required(ErrorMessage = "Conversation ID là bắt buộc")]
    public int ConversationId { get; set; }
    
    [Required(ErrorMessage = "Cần ít nhất một thành viên")]
    [MinLength(1, ErrorMessage = "Cần ít nhất một thành viên")]
    public List<int> MemberIds { get; set; } = new();
}

/// <summary>
/// DTO for adding members body (without conversationId in route)
/// </summary>
public class AddMembersBodyRequest
{
    [Required(ErrorMessage = "Cần ít nhất một thành viên")]
    [MinLength(1, ErrorMessage = "Cần ít nhất một thành viên")]
    public List<int> MemberIds { get; set; } = new();
}

/// <summary>
/// DTO for removing member from group
/// </summary>
public class RemoveMemberRequest
{
    [Required(ErrorMessage = "Conversation ID là bắt buộc")]
    public int ConversationId { get; set; }
    
    [Required(ErrorMessage = "Member ID là bắt buộc")]
    public int MemberId { get; set; }
}
