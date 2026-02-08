namespace capstone_backend.Business.DTOs.Messaging;

/// <summary>
/// DTO for conversation response
/// </summary>
public class ConversationResponse
{
    public int Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public string? Name { get; set; }
    public int? CreatedBy { get; set; }
    public DateTime? CreatedAt { get; set; }
    public List<ConversationMemberResponse> Members { get; set; } = new();
    public MessageResponse? LastMessage { get; set; }
    public int UnreadCount { get; set; }
}

/// <summary>
/// DTO for conversation member response
/// </summary>
public class ConversationMemberResponse
{
    public int UserId { get; set; }
    public string? Username { get; set; }
    public string? FullName { get; set; }
    public string? Avatar { get; set; }
    public string Role { get; set; } = string.Empty;
    public DateTime? JoinedAt { get; set; }
    public bool IsOnline { get; set; }
}

/// <summary>
/// DTO for message response
/// </summary>
public class MessageResponse
{
    public int Id { get; set; }
    public int ConversationId { get; set; }
    public int SenderId { get; set; }
    public string? SenderName { get; set; }
    public string? SenderAvatar { get; set; }
    public string? Content { get; set; }
    public string MessageType { get; set; } = string.Empty;
    public int? ReferenceId { get; set; }
    public string? ReferenceType { get; set; }
    public string? Metadata { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public bool IsMine { get; set; }
}

/// <summary>
/// DTO for paginated messages response
/// </summary>
public class MessagesPageResponse
{
    public List<MessageResponse> Messages { get; set; } = new();
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
    public bool HasNextPage { get; set; }
}

/// <summary>
/// DTO for typing indicator event
/// </summary>
public class TypingIndicatorResponse
{
    public int ConversationId { get; set; }
    public int UserId { get; set; }
    public string? Username { get; set; }
    public bool IsTyping { get; set; }
}

/// <summary>
/// DTO for online status
/// </summary>
public class OnlineStatusResponse
{
    public int UserId { get; set; }
    public bool IsOnline { get; set; }
    public DateTime? LastSeen { get; set; }
}
