using capstone_backend.Business.DTOs.Messaging;
using capstone_backend.Business.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;
using System.Security.Claims;

namespace capstone_backend.Hubs;

/// <summary>
/// SignalR Hub for real-time messaging with typing indicators and online status
/// </summary>
[Authorize]
public class MessagingHub : Hub
{
    private readonly IConversationRepository _conversationRepository;
    private static readonly ConcurrentDictionary<int, HashSet<string>> UserConnections = new();
    private static readonly ConcurrentDictionary<string, DateTime> TypingUsers = new();

    public MessagingHub(IConversationRepository conversationRepository)
    {
        _conversationRepository = conversationRepository;
    }

    /// <summary>
    /// Called when user connects
    /// </summary>
    public override async Task OnConnectedAsync()
    {
        var userId = GetCurrentUserId();
        if (userId > 0)
        {
            // Track connection
            UserConnections.AddOrUpdate(
                userId,
                new HashSet<string> { Context.ConnectionId },
                (key, existingSet) =>
                {
                    existingSet.Add(Context.ConnectionId);
                    return existingSet;
                });

            // Notify others that user is online
            await Clients.Others.SendAsync("UserOnline", userId);
        }

        await base.OnConnectedAsync();
    }

    /// <summary>
    /// Called when user disconnects
    /// </summary>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = GetCurrentUserId();
        if (userId > 0)
        {
            if (UserConnections.TryGetValue(userId, out var connections))
            {
                connections.Remove(Context.ConnectionId);
                
                // If no more connections, user is offline
                if (connections.Count == 0)
                {
                    UserConnections.TryRemove(userId, out _);
                    await Clients.Others.SendAsync("UserOffline", userId, DateTime.UtcNow);
                }
            }

            // Clear typing indicator
            var typingKey = $"{userId}";
            TypingUsers.TryRemove(typingKey, out _);
        }

        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Join a conversation room
    /// </summary>
    public async Task JoinConversation(int conversationId)
    {
        var userId = GetCurrentUserId();
        if (userId <= 0 || conversationId <= 0)
            return;

        // Verify user is member
        var isMember = await _conversationRepository.IsUserMemberAsync(conversationId, userId);
        if (!isMember)
            throw new HubException("You are not a member of this conversation");

        var groupName = GetConversationGroupName(conversationId);
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
    }

    /// <summary>
    /// Leave a conversation room
    /// </summary>
    public async Task LeaveConversation(int conversationId)
    {
        if (conversationId <= 0)
            return;

        var groupName = GetConversationGroupName(conversationId);
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
    }

    /// <summary>
    /// Send typing indicator
    /// </summary>
    public async Task SendTypingIndicator(int conversationId, bool isTyping)
    {
        var userId = GetCurrentUserId();
        if (userId <= 0 || conversationId <= 0)
            return;

        // Verify user is member
        var isMember = await _conversationRepository.IsUserMemberAsync(conversationId, userId);
        if (!isMember)
            return;

        var groupName = GetConversationGroupName(conversationId);
        var typingKey = $"{userId}_{conversationId}";

        if (isTyping)
        {
            TypingUsers[typingKey] = DateTime.UtcNow;
            
            // Send to conversation members (except self)
            await Clients.OthersInGroup(groupName).SendAsync("UserTyping", new TypingIndicatorResponse
            {
                ConversationId = conversationId,
                UserId = userId,
                Username = Context.User?.FindFirst(ClaimTypes.Name)?.Value,
                IsTyping = true
            });

            // Auto-clear after 3 seconds
            _ = Task.Run(async () =>
            {
                await Task.Delay(3000);
                if (TypingUsers.TryGetValue(typingKey, out var timestamp))
                {
                    if ((DateTime.UtcNow - timestamp).TotalSeconds >= 3)
                    {
                        TypingUsers.TryRemove(typingKey, out _);
                        await Clients.OthersInGroup(groupName).SendAsync("UserTyping", new TypingIndicatorResponse
                        {
                            ConversationId = conversationId,
                            UserId = userId,
                            IsTyping = false
                        });
                    }
                }
            });
        }
        else
        {
            TypingUsers.TryRemove(typingKey, out _);
            await Clients.OthersInGroup(groupName).SendAsync("UserTyping", new TypingIndicatorResponse
            {
                ConversationId = conversationId,
                UserId = userId,
                Username = Context.User?.FindFirst(ClaimTypes.Name)?.Value,
                IsTyping = false
            });
        }
    }

    /// <summary>
    /// Get online status of users
    /// </summary>
    public async Task<List<OnlineStatusResponse>> GetOnlineStatus(List<int> userIds)
    {
        var result = new List<OnlineStatusResponse>();
        
        foreach (var userId in userIds)
        {
            result.Add(new OnlineStatusResponse
            {
                UserId = userId,
                IsOnline = UserConnections.ContainsKey(userId),
                LastSeen = null // Can implement last seen tracking if needed
            });
        }

        return await Task.FromResult(result);
    }

    /// <summary>
    /// Send message received confirmation
    /// </summary>
    public async Task ConfirmMessageReceived(int messageId)
    {
        var userId = GetCurrentUserId();
        if (userId <= 0 || messageId <= 0)
            return;

        // Can implement delivery receipt logic here
        await Task.CompletedTask;
    }

    // Helper methods
    private int GetCurrentUserId()
    {
        var userIdClaim = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (int.TryParse(userIdClaim, out var userId))
            return userId;
        return 0;
    }

    private static string GetConversationGroupName(int conversationId)
    {
        return $"conversation_{conversationId}";
    }

    /// <summary>
    /// Check if user is online
    /// </summary>
    public static bool IsUserOnline(int userId)
    {
        return UserConnections.ContainsKey(userId);
    }
}
