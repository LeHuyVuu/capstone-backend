using capstone_backend.Business.Interfaces;
using capstone_backend.Data.Context;
using capstone_backend.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace capstone_backend.Data.Repositories;

/// <summary>
/// Repository implementation for Conversation entity
/// </summary>
public class ConversationRepository : GenericRepository<Conversation>, IConversationRepository
{
    public ConversationRepository(MyDbContext context) : base(context)
    {
    }

    public async Task<Conversation?> GetByIdWithMembersAsync(
        int conversationId, 
        CancellationToken cancellationToken = default)
    {
        if (conversationId <= 0)
            return null;

        return await _context.Conversations
            .Include(c => c.Members!.Where(m => m.IsActive == true))
            .ThenInclude(m => m.User)
            .FirstOrDefaultAsync(c => c.Id == conversationId && c.IsDeleted == false, cancellationToken);
    }

    public async Task<Conversation?> GetDirectConversationAsync(
        int userId1, 
        int userId2, 
        CancellationToken cancellationToken = default)
    {
        if (userId1 <= 0 || userId2 <= 0 || userId1 == userId2)
            return null;

        // Tìm conversation DIRECT có đúng 2 users này
        return await _context.Conversations
            .Where(c => c.Type == "DIRECT" && c.IsDeleted == false)
            .Where(c => c.Members != null && c.Members.Count(m => m.IsActive == true) == 2)
            .Where(c => c.Members!.Any(m => m.UserId == userId1 && m.IsActive == true) 
                     && c.Members!.Any(m => m.UserId == userId2 && m.IsActive == true))
            .Include(c => c.Members!.Where(m => m.IsActive == true))
            .ThenInclude(m => m.User)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<List<Conversation>> GetUserConversationsAsync(
        int userId, 
        CancellationToken cancellationToken = default)
    {
        if (userId <= 0)
            return new List<Conversation>();

        return await _context.Conversations
            .Where(c => c.IsDeleted == false)
            .Where(c => c.Members != null && c.Members.Any(m => m.UserId == userId && m.IsActive == true))
            .Include(c => c.Members!.Where(m => m.IsActive == true))
            .ThenInclude(m => m.User)
            .Include(c => c.Messages!.Where(m => m.IsDeleted == false).OrderByDescending(m => m.CreatedAt).Take(1))
            .OrderByDescending(c => c.Messages!.Any() 
                ? c.Messages!.Max(m => m.CreatedAt) 
                : c.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> IsUserMemberAsync(
        int conversationId, 
        int userId, 
        CancellationToken cancellationToken = default)
    {
        if (conversationId <= 0 || userId <= 0)
            return false;

        return await _context.ConversationMembers
            .AnyAsync(cm => cm.ConversationId == conversationId 
                         && cm.UserId == userId 
                         && cm.IsActive == true, 
                cancellationToken);
    }
}
