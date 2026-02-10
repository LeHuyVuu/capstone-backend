using capstone_backend.Data.Entities;

namespace capstone_backend.Business.Interfaces;

/// <summary>
/// Repository interface for ConversationMember entity
/// </summary>
public interface IConversationMemberRepository : IGenericRepository<ConversationMember>
{
    /// <summary>
    /// Get active members of a conversation
    /// </summary>
    Task<List<ConversationMember>> GetActiveConversationMembersAsync(int conversationId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get member record for a user in conversation
    /// </summary>
    Task<ConversationMember?> GetMemberAsync(int conversationId, int userId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Update last read message for user
    /// </summary>
    Task UpdateLastReadMessageAsync(int conversationId, int userId, int messageId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get unread count for user in conversation
    /// </summary>
    Task<int> GetUnreadCountAsync(int conversationId, int userId, CancellationToken cancellationToken = default);
}
