using capstone_backend.Business.DTOs.Messaging;
using capstone_backend.Business.Exceptions;
using capstone_backend.Business.Interfaces;
using capstone_backend.Data.Entities;
using capstone_backend.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace capstone_backend.Business.Services;

/// <summary>
/// Service implementation for messaging operations with comprehensive validation
/// </summary>
public class MessagingService : IMessagingService
{
    private readonly IConversationRepository _conversationRepository;
    private readonly IConversationMemberRepository _memberRepository;
    private readonly IMessageRepository _messageRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IHubContext<MessagingHub> _hubContext;

    public MessagingService(
        IConversationRepository conversationRepository,
        IConversationMemberRepository memberRepository,
        IMessageRepository messageRepository,
        IUnitOfWork unitOfWork,
        IHubContext<MessagingHub> hubContext)
    {
        _conversationRepository = conversationRepository;
        _memberRepository = memberRepository;
        _messageRepository = messageRepository;
        _unitOfWork = unitOfWork;
        _hubContext = hubContext;
    }

    public async Task<ConversationResponse> CreateConversationAsync(
        int currentUserId, 
        CreateConversationRequest request, 
        CancellationToken cancellationToken = default)
    {
        // Validation
        if (currentUserId <= 0)
            throw new Exception("Invalid user");

        if (request.MemberIds == null || !request.MemberIds.Any())
            throw new Exception("At least one member is required");

        // Remove duplicates and ensure current user is included
        var memberIds = request.MemberIds.Distinct().ToList();
        if (!memberIds.Contains(currentUserId))
            memberIds.Add(currentUserId);

        // For direct conversation, must have exactly 2 members
        if (request.Type == "DIRECT")
        {
            if (memberIds.Count != 2)
                throw new Exception("Direct conversation must have exactly 2 members");

            // Check if conversation already exists
            var otherUserId = memberIds.First(id => id != currentUserId);
            var existing = await _conversationRepository.GetDirectConversationAsync(currentUserId, otherUserId, cancellationToken);
            if (existing != null)
                return await MapToConversationResponse(existing, currentUserId, cancellationToken);
        }
        else if (request.Type == "GROUP")
        {
            if (memberIds.Count < 2)
                throw new Exception("Group conversation must have at least 2 members");

            if (string.IsNullOrWhiteSpace(request.Name))
                throw new Exception("Group name is required");
        }

        // Create conversation
        var conversation = new Conversation
        {
            Type = request.Type,
            Name = request.Name?.Trim(),
            CreatedBy = currentUserId,
            CreatedAt = DateTime.UtcNow,
            IsDeleted = false,
            Members = new List<ConversationMember>()
        };

        // Add members
        foreach (var memberId in memberIds)
        {
            conversation.Members.Add(new ConversationMember
            {
                UserId = memberId,
                Role = memberId == currentUserId ? "ADMIN" : "MEMBER",
                JoinedAt = DateTime.UtcNow,
                IsActive = true
            });
        }

        await _conversationRepository.AddAsync(conversation);
        await _unitOfWork.SaveChangesAsync();

        // Notify members via SignalR
        foreach (var memberId in memberIds.Where(id => id != currentUserId))
        {
            await _hubContext.Clients.User(memberId.ToString())
                .SendAsync("NewConversation", conversation.Id, cancellationToken);
        }

        return await MapToConversationResponse(conversation, currentUserId, cancellationToken);
    }

    public async Task<ConversationResponse> GetOrCreateDirectConversationAsync(
        int currentUserId, 
        int otherUserId, 
        CancellationToken cancellationToken = default)
    {
        // Validation
        if (currentUserId <= 0)
            throw new Exception("Invalid user");

        if (otherUserId <= 0)
            throw new Exception("Invalid other user ID");

        if (currentUserId == otherUserId)
            throw new Exception("Cannot create conversation with yourself");

        // Check if conversation exists
        var existing = await _conversationRepository.GetDirectConversationAsync(currentUserId, otherUserId, cancellationToken);
        if (existing != null)
            return await MapToConversationResponse(existing, currentUserId, cancellationToken);

        // Create new conversation
        var request = new CreateConversationRequest
        {
            Type = "DIRECT",
            MemberIds = new List<int> { otherUserId }
        };

        return await CreateConversationAsync(currentUserId, request, cancellationToken);
    }

    public async Task<List<ConversationResponse>> GetUserConversationsAsync(
        int currentUserId, 
        CancellationToken cancellationToken = default)
    {
        if (currentUserId <= 0)
            throw new Exception("Invalid user");

        var conversations = await _conversationRepository.GetUserConversationsAsync(currentUserId, cancellationToken);
        
        var result = new List<ConversationResponse>();
        foreach (var conversation in conversations)
        {
            result.Add(await MapToConversationResponse(conversation, currentUserId, cancellationToken));
        }

        return result;
    }

    public async Task<ConversationResponse> GetConversationByIdAsync(
        int currentUserId, 
        int conversationId, 
        CancellationToken cancellationToken = default)
    {
        if (currentUserId <= 0)
            throw new Exception("Invalid user");

        if (conversationId <= 0)
            throw new Exception("Invalid conversation ID");

        // Check if user is member
        var isMember = await _conversationRepository.IsUserMemberAsync(conversationId, currentUserId, cancellationToken);
        if (!isMember)
            throw new Exception("You are not a member of this conversation");

        var conversation = await _conversationRepository.GetByIdWithMembersAsync(conversationId, cancellationToken);
        if (conversation == null)
            throw new Exception("Conversation not found");

        return await MapToConversationResponse(conversation, currentUserId, cancellationToken);
    }

    public async Task<MessageResponse> SendMessageAsync(
        int currentUserId, 
        SendMessageRequest request, 
        CancellationToken cancellationToken = default)
    {
        // Validation
        if (currentUserId <= 0)
            throw new Exception("Invalid user");

        if (request.ConversationId <= 0)
            throw new Exception("Invalid conversation ID");

        // Check if user is member
        var isMember = await _conversationRepository.IsUserMemberAsync(request.ConversationId, currentUserId, cancellationToken);
        if (!isMember)
            throw new Exception("You are not a member of this conversation");

        // Validate content based on message type
        if (request.MessageType == "TEXT" && string.IsNullOrWhiteSpace(request.Content))
            throw new Exception("Message content is required for text messages");

        if (request.MessageType != "TEXT" && request.ReferenceId == null)
            throw new Exception($"Reference ID is required for {request.MessageType} messages");

        // Create message
        var message = new Message
        {
            ConversationId = request.ConversationId,
            SenderId = currentUserId,
            Content = request.Content?.Trim(),
            MessageType = request.MessageType,
            ReferenceId = request.ReferenceId,
            ReferenceType = request.ReferenceType?.Trim(),
            Metadata = request.Metadata?.Trim(),
            CreatedAt = DateTime.UtcNow,
            IsDeleted = false
        };

        await _messageRepository.AddAsync(message);
        await _unitOfWork.SaveChangesAsync();

        // Load sender info
        var messageWithSender = await _messageRepository.GetByIdWithSenderAsync(message.Id, cancellationToken);
        if (messageWithSender == null)
            throw new Exception("Message not found after creation");

        // Notify conversation members via SignalR - create response for each member with correct IsMine
        var members = await _memberRepository.GetActiveConversationMembersAsync(request.ConversationId, cancellationToken);
        foreach (var member in members)
        {
            if (member.UserId == null || member.UserId == currentUserId)
                continue;
                
            // Create response specific to this member so IsMine is correct
            var memberResponse = MapToMessageResponse(messageWithSender, member.UserId.Value);
            await _hubContext.Clients.User(member.UserId.Value.ToString())
                .SendAsync("ReceiveMessage", memberResponse, cancellationToken);
        }

        // Return response for sender with IsMine = true
        var response = MapToMessageResponse(messageWithSender, currentUserId);
        return response;
    }

    public async Task<MessagesPageResponse> GetMessagesAsync(
        int currentUserId, 
        int conversationId, 
        int pageNumber = 1, 
        int pageSize = 50, 
        CancellationToken cancellationToken = default)
    {
        // Validation
        if (currentUserId <= 0)
            throw new Exception("Invalid user");

        if (conversationId <= 0)
            throw new Exception("Invalid conversation ID");

        // Check if user is member
        var isMember = await _conversationRepository.IsUserMemberAsync(conversationId, currentUserId, cancellationToken);
        if (!isMember)
            throw new Exception("You are not a member of this conversation");

        // Sanitize pagination
        pageNumber = Math.Max(1, pageNumber);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var messages = await _messageRepository.GetConversationMessagesAsync(
            conversationId, pageNumber, pageSize, cancellationToken);

        return new MessagesPageResponse
        {
            Messages = messages.Select(m => MapToMessageResponse(m, currentUserId)).ToList(),
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalPages = 0, // Can calculate if needed
            HasNextPage = messages.Count == pageSize
        };
    }

    public async Task MarkAsReadAsync(
        int currentUserId, 
        MarkAsReadRequest request, 
        CancellationToken cancellationToken = default)
    {
        // Validation
        if (currentUserId <= 0)
            throw new Exception("Invalid user");

        if (request.ConversationId <= 0 || request.MessageId <= 0)
            throw new Exception("Invalid conversation or message ID");

        // Check if user is member
        var isMember = await _conversationRepository.IsUserMemberAsync(request.ConversationId, currentUserId, cancellationToken);
        if (!isMember)
            throw new Exception("You are not a member of this conversation");

        await _memberRepository.UpdateLastReadMessageAsync(
            request.ConversationId, currentUserId, request.MessageId, cancellationToken);

        // Notify sender via SignalR
        var message = await _messageRepository.GetByIdAsync(request.MessageId);
        if (message?.SenderId != null && message.SenderId != currentUserId)
        {
            await _hubContext.Clients.User(message.SenderId.Value.ToString())
                .SendAsync("MessageRead", request.ConversationId, request.MessageId, currentUserId, cancellationToken);
        }
    }

    public async Task AddMembersAsync(
        int currentUserId, 
        AddMembersRequest request, 
        CancellationToken cancellationToken = default)
    {
        // Validation
        if (currentUserId <= 0)
            throw new Exception("Invalid user");

        if (request.ConversationId <= 0)
            throw new Exception("Invalid conversation ID");

        if (request.MemberIds == null || !request.MemberIds.Any())
            throw new Exception("At least one member is required");

        // Check conversation exists and is group
        var conversation = await _conversationRepository.GetByIdWithMembersAsync(request.ConversationId, cancellationToken);
        if (conversation == null)
            throw new Exception("Conversation not found");

        if (conversation.Type != "GROUP")
            throw new Exception("Can only add members to group conversations");

        // Check if current user is admin
        var currentMember = await _memberRepository.GetMemberAsync(request.ConversationId, currentUserId, cancellationToken);
        if (currentMember?.Role != "ADMIN" || currentMember?.IsActive != true)
            throw new Exception("Only admins can add members");

        // Add new members
        var memberIds = request.MemberIds.Distinct().ToList();
        foreach (var memberId in memberIds)
        {
            // Check if already member
            var existing = await _memberRepository.GetMemberAsync(request.ConversationId, memberId, cancellationToken);
            if (existing == null)
            {
                var newMember = new ConversationMember
                {
                    ConversationId = request.ConversationId,
                    UserId = memberId,
                    Role = "MEMBER",
                    JoinedAt = DateTime.UtcNow,
                    IsActive = true
                };
                await _memberRepository.AddAsync(newMember);
            }
            else if (existing.IsActive == false)
            {
                // Reactivate member
                existing.IsActive = true;
                existing.JoinedAt = DateTime.UtcNow;
                _memberRepository.Update(existing);
            }
        }

        await _unitOfWork.SaveChangesAsync();

        // Notify new members via SignalR
        foreach (var memberId in memberIds)
        {
            await _hubContext.Clients.User(memberId.ToString())
                .SendAsync("AddedToConversation", request.ConversationId, cancellationToken);
        }
    }

    public async Task RemoveMemberAsync(
        int currentUserId, 
        RemoveMemberRequest request, 
        CancellationToken cancellationToken = default)
    {
        // Validation
        if (currentUserId <= 0)
            throw new Exception("Invalid user");

        if (request.ConversationId <= 0 || request.UserId <= 0)
            throw new Exception("Invalid conversation or user ID");

        // Check conversation exists and is group
        var conversation = await _conversationRepository.GetByIdAsync(request.ConversationId);
        if (conversation == null)
            throw new Exception("Conversation not found");

        if (conversation.Type != "GROUP")
            throw new Exception("Can only remove members from group conversations");

        // Check if current user is admin (unless removing self)
        if (currentUserId != request.UserId)
        {
            var currentMember = await _memberRepository.GetMemberAsync(request.ConversationId, currentUserId, cancellationToken);
            if (currentMember?.Role != "ADMIN" || currentMember?.IsActive != true)
                throw new Exception("Only admins can remove members");
        }

        // Remove member
        var member = await _memberRepository.GetMemberAsync(request.ConversationId, request.UserId, cancellationToken);
        if (member == null || member.IsActive == false)
            throw new Exception("Member not found");

        member.IsActive = false;
        _memberRepository.Update(member);
        await _unitOfWork.SaveChangesAsync();

         // Notify removed member via SignalR
        await _hubContext.Clients.User(request.UserId.ToString())
            .SendAsync("RemovedFromConversation", request.ConversationId, cancellationToken);
    }

    public async Task LeaveConversationAsync(
        int currentUserId, 
        int conversationId, 
        CancellationToken cancellationToken = default)
    {
        await RemoveMemberAsync(currentUserId, new RemoveMemberRequest
        {
            ConversationId = conversationId,
            UserId = currentUserId
        }, cancellationToken);
    }

    public async Task DeleteMessageAsync(
        int currentUserId, 
        int messageId, 
        CancellationToken cancellationToken = default)
    {
        // Validation
        if (currentUserId <= 0)
            throw new Exception("Invalid user");

        if (messageId <= 0)
            throw new Exception("Invalid message ID");

        var message = await _messageRepository.GetByIdAsync(messageId);
        if (message == null || message.IsDeleted == true)
            throw new Exception("Message not found");

        // Only sender can delete
        if (message.SenderId != currentUserId)
            throw new Exception("You can only delete your own messages");

        message.IsDeleted = true;
        message.UpdatedAt = DateTime.UtcNow;
        _messageRepository.Update(message);
        await _unitOfWork.SaveChangesAsync();

        // Notify conversation members via SignalR
        if (message.ConversationId != null)
        {
            var members = await _memberRepository.GetActiveConversationMembersAsync(message.ConversationId.Value, cancellationToken);
            foreach (var member in members)
            {
                if (member.UserId == null || member.UserId == currentUserId)
                    continue;
                    
                await _hubContext.Clients.User(member.UserId.Value.ToString())
                    .SendAsync("MessageDeleted", messageId, cancellationToken);
            }
        }
    }

    public async Task<List<MessageResponse>> SearchMessagesAsync(
        int currentUserId, 
        int conversationId, 
        string searchTerm, 
        CancellationToken cancellationToken = default)
    {
        // Validation
        if (currentUserId <= 0)
            throw new Exception("Invalid user");

        if (conversationId <= 0)
            throw new Exception("Invalid conversation ID");

        if (string.IsNullOrWhiteSpace(searchTerm))
            throw new Exception("Search term is required");

        // Check if user is member
        var isMember = await _conversationRepository.IsUserMemberAsync(conversationId, currentUserId, cancellationToken);
        if (!isMember)
            throw new Exception("You are not a member of this conversation");

        var messages = await _messageRepository.SearchMessagesAsync(conversationId, searchTerm, cancellationToken);
        return messages.Select(m => MapToMessageResponse(m, currentUserId)).ToList();
    }

    // Helper methods
    private async Task<ConversationResponse> MapToConversationResponse(
        Conversation conversation, 
        int currentUserId, 
        CancellationToken cancellationToken)
    {
        var response = new ConversationResponse
        {
            Id = conversation.Id,
            Type = conversation.Type ?? "DIRECT",
            Name = conversation.Name,
            CreatedBy = conversation.CreatedBy,
            CreatedAt = conversation.CreatedAt,
            Members = conversation.Members?.Where(m => m.IsActive == true).Select(m => new ConversationMemberResponse
            {
                UserId = m.UserId ?? 0,
                Username = m.User?.Email,
                FullName = !string.IsNullOrWhiteSpace(m.User?.DisplayName) 
                    ? m.User.DisplayName 
                    : m.User?.Email,
                Avatar = m.User?.AvatarUrl,
                Role = m.Role ?? "MEMBER",
                JoinedAt = m.JoinedAt,
                IsOnline = false // TODO: Implement online status tracking
            }).ToList() ?? new List<ConversationMemberResponse>()
        };

        // Get last message
        var lastMessage = conversation.Messages?.OrderByDescending(m => m.CreatedAt).FirstOrDefault();
        if (lastMessage != null)
        {
            response.LastMessage = MapToMessageResponse(lastMessage, currentUserId);
        }

        // Get unread count
        response.UnreadCount = await _memberRepository.GetUnreadCountAsync(conversation.Id, currentUserId, cancellationToken);

        return response;
    }

    private MessageResponse MapToMessageResponse(Message message, int currentUserId)
    {
        return new MessageResponse
        {
            Id = message.Id,
            ConversationId = message.ConversationId ?? 0,
            SenderId = message.SenderId ?? 0,
            SenderName = !string.IsNullOrWhiteSpace(message.Sender?.DisplayName)
                ? message.Sender.DisplayName
                : message.Sender?.Email,
            SenderAvatar = message.Sender?.AvatarUrl,
            Content = message.Content,
            MessageType = message.MessageType ?? "TEXT",
            ReferenceId = message.ReferenceId,
            ReferenceType = message.ReferenceType,
            Metadata = message.Metadata,
            CreatedAt = message.CreatedAt,
            UpdatedAt = message.UpdatedAt,
            IsMine = message.SenderId == currentUserId
        };
    }
}
