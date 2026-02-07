using capstone_backend.Data.Entities;

namespace capstone_backend.Business.Interfaces;

/// <summary>
/// Repository interface for Conversation entity
/// </summary>
public interface IConversationRepository : IGenericRepository<Conversation>
{
    /// <summary>
    /// Get conversation by ID with members included
    /// </summary>
    Task<Conversation?> GetByIdWithMembersAsync(int conversationId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get direct conversation between two users
    /// </summary>
    Task<Conversation?> GetDirectConversationAsync(int userId1, int userId2, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get all conversations for a user
    /// </summary>
    Task<List<Conversation>> GetUserConversationsAsync(int userId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Check if user is member of conversation
    /// </summary>
    Task<bool> IsUserMemberAsync(int conversationId, int userId, CancellationToken cancellationToken = default);
}
