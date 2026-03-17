using capstone_backend.Business.DTOs.Messaging;
using capstone_backend.Business.DTOs.Notification;
using capstone_backend.Business.Exceptions;
using capstone_backend.Business.Interfaces;
using capstone_backend.Data.Entities;
using capstone_backend.Data.Interfaces;
using capstone_backend.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

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
    private readonly IFcmService _fcmService;
    private readonly IDeviceTokenRepository _deviceTokenRepository;

    public MessagingService(
        IConversationRepository conversationRepository,
        IConversationMemberRepository memberRepository,
        IMessageRepository messageRepository,
        IUnitOfWork unitOfWork,
        IHubContext<MessagingHub> hubContext,
        IFcmService fcmService,
        IDeviceTokenRepository deviceTokenRepository)
    {
        _conversationRepository = conversationRepository;
        _memberRepository = memberRepository;
        _messageRepository = messageRepository;
        _unitOfWork = unitOfWork;
        _hubContext = hubContext;
        _fcmService = fcmService;
        _deviceTokenRepository = deviceTokenRepository;
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

        // Validate all member users exist in database
        foreach (var memberId in memberIds)
        {
            var user = await _unitOfWork.Users.GetByIdAsync(memberId);
            if (user == null)
                throw new BadRequestException($"User with ID {memberId} not found", "USER_NOT_FOUND");
        }

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

        // Load lại conversation với đầy đủ User info
        var createdConversation = await _conversationRepository.GetByIdWithMembersAsync(conversation.Id, cancellationToken);
        if (createdConversation == null)
            throw new Exception("Failed to load created conversation");

        var response = await MapToConversationResponse(createdConversation, currentUserId, cancellationToken);

        // Notify members via SignalR - gửi full conversation data
        foreach (var memberId in memberIds.Where(id => id != currentUserId))
        {
            // Map response cho từng member để IsMine và OtherUser đúng
            var memberResponse = await MapToConversationResponse(createdConversation, memberId, cancellationToken);
            await _hubContext.Clients.User(memberId.ToString())
                .SendAsync("NewConversation", memberResponse, cancellationToken);
        }

        return response;
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

        // Validate other user exists
        var otherUser = await _unitOfWork.Users.GetByIdAsync(otherUserId);
        if (otherUser == null)
            throw new BadRequestException($"User with ID {otherUserId} not found", "USER_NOT_FOUND");

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

    public async Task<ConversationResponse> GetCoupleConversationAsync(
        int currentUserId, 
        CancellationToken cancellationToken = default)
    {
        if (currentUserId <= 0)
            throw new BadRequestException("User ID không hợp lệ", "INVALID_USER");

        var user = await _unitOfWork.Users.GetByIdAsync(currentUserId);
        if (user == null)
            throw new BadRequestException("Không tìm thấy user", "USER_NOT_FOUND");

        var memberProfile = await _unitOfWork.MembersProfile.GetByUserIdAsync(currentUserId);
        if (memberProfile == null)
            throw new BadRequestException("Không tìm thấy member profile", "MEMBER_PROFILE_NOT_FOUND");

        var couple = await _unitOfWork.CoupleProfiles.GetActiveCoupleByMemberIdAsync(memberProfile.Id);
        if (couple == null)
            throw new BadRequestException("Bạn chưa có cặp đôi hoặc đang ở trạng thái SINGLE", "NO_ACTIVE_COUPLE");

        var partnerMemberId = couple.MemberId1 == memberProfile.Id ? couple.MemberId2 : couple.MemberId1;

        if (partnerMemberId <= 0)
            throw new BadRequestException("Partner member ID không hợp lệ", "INVALID_PARTNER");

        var partnerMember = await _unitOfWork.MembersProfile.GetByIdAsync(partnerMemberId);
        if (partnerMember == null)
            throw new BadRequestException("Không tìm thấy người yêu của bạn", "PARTNER_NOT_FOUND");

        return await GetOrCreateDirectConversationAsync(currentUserId, partnerMember.UserId, cancellationToken);
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
            throw new BadRequestException("Message content is required for text messages", "CONTENT_REQUIRED");

        if ((request.MessageType == "IMAGE" || request.MessageType == "FILE" || 
             request.MessageType == "VIDEO" || request.MessageType == "AUDIO") && 
            string.IsNullOrWhiteSpace(request.FileUrl))
            throw new BadRequestException($"File URL is required for {request.MessageType} messages. Please upload the file first using /api/upload endpoint.", "FILE_URL_REQUIRED");

        // Build metadata JSON for file attachments or date plan
        string? metadata = request.Metadata;
        
        // Populate DatePlan info into metadata for rich card display (check DATE_PLAN first)
        if (request.ReferenceType == "DATE_PLAN" && request.ReferenceId.HasValue)
        {
            Console.WriteLine($"[DEBUG] Loading DatePlan ID: {request.ReferenceId.Value}");
            
            // Query DatePlan directly - trust the user has permission if they know the ID
            var datePlan = await _unitOfWork.Context.DatePlans
                .AsNoTracking()
                .Include(dp => dp.DatePlanItems.Where(dpi => dpi.IsDeleted == false).OrderBy(dpi => dpi.OrderIndex))
                    .ThenInclude(dpi => dpi.VenueLocation)
                .Where(dp => dp.Id == request.ReferenceId.Value 
                          && dp.IsDeleted == false)
                .FirstOrDefaultAsync(cancellationToken);

            Console.WriteLine($"[DEBUG] DatePlan found: {datePlan != null}, Items count: {datePlan?.DatePlanItems?.Count ?? 0}");

            if (datePlan != null)
            {
                var firstVenue = datePlan.DatePlanItems?.FirstOrDefault()?.VenueLocation;
                
                // Extract first image from comma-separated CoverImage string
                var imageUrl = firstVenue?.CoverImage;
                if (!string.IsNullOrWhiteSpace(imageUrl) && imageUrl.Contains(','))
                {
                    imageUrl = imageUrl.Split(',')[0].Trim();
                }
                
                var datePlanInfo = new
                {
                    datePlanId = datePlan.Id,
                    title = datePlan.Title,
                    status = datePlan.Status,
                    plannedStartAt = datePlan.PlannedStartAt,
                    plannedEndAt = datePlan.PlannedEndAt,
                    estimatedBudget = datePlan.EstimatedBudget,
                    totalCount = datePlan.DatePlanItems?.Count ?? 0,
                    imageDatePlanUrl = "https://couplemood-store.s3.ap-southeast-2.amazonaws.com/images/46/e783d6de-d417-4bb7-88a4-9ddce30bd8bc.jpg"
                };
                metadata = System.Text.Json.JsonSerializer.Serialize(datePlanInfo);
                Console.WriteLine($"[DEBUG] DatePlan metadata serialized: {metadata}");
            }
            else
            {
                Console.WriteLine($"[DEBUG] DatePlan ID {request.ReferenceId.Value} NOT FOUND or IsDeleted=true");
            }
        }
        // Handle file attachments (images, videos, files, audio)
        else if (!string.IsNullOrWhiteSpace(request.FileUrl))
        {
            Console.WriteLine($"[DEBUG] File attachment detected");
            var fileInfo = new
            {
                FileUrl = request.FileUrl,
                FileName = request.FileName,
                FileSize = request.FileSize
            };
            metadata = System.Text.Json.JsonSerializer.Serialize(fileInfo);
        }
        else
        {
            Console.WriteLine($"[DEBUG] No DATE_PLAN or FileUrl: ReferenceType={request.ReferenceType}, ReferenceId={request.ReferenceId}");
        }

        // Auto-generate content for media messages
        string? messageContent = request.Content?.Trim();
        if (string.IsNullOrWhiteSpace(messageContent))
        {
            messageContent = request.MessageType switch
            {
                "IMAGE" => "đã gửi 1 ảnh",
                "VIDEO" => "đã gửi 1 video",
                "AUDIO" => "đã gửi 1 tin nhắn thoại",
                "FILE" => "đã gửi 1 tệp",
                _ => messageContent
            };
        }

        // Create message
        var message = new Message
        {
            ConversationId = request.ConversationId,
            SenderId = currentUserId,
            Content = messageContent,
            MessageType = request.MessageType,
            ReferenceId = request.ReferenceId,
            ReferenceType = request.ReferenceType?.Trim(),
            Metadata = metadata,
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
        
        // Collect all member user IDs (excluding sender) for notification
        var memberUserIds = new List<int>();
        
        foreach (var member in members)
        {
            if (member.UserId == null || member.UserId == currentUserId)
                continue;
                
            memberUserIds.Add(member.UserId.Value);
            
            // Create response specific to this member so IsMine is correct
            var memberResponse = await MapToMessageResponseAsync(messageWithSender, member.UserId.Value);
            await _hubContext.Clients.User(member.UserId.Value.ToString())
                .SendAsync("ReceiveMessage", memberResponse, cancellationToken);
        }

        // Send push notification to members who are not online
        if (memberUserIds.Any())
        {
            try
            {
                // Get conversation info to check if it's a group
                var conversation = await _conversationRepository.GetByIdAsync(request.ConversationId);
                var isGroupConversation = conversation?.Type == "GROUP";
                
                // Get sender info for notification
                var sender = await _unitOfWork.Users.GetByIdAsync(currentUserId);
                var senderName = !string.IsNullOrWhiteSpace(sender?.DisplayName) 
                    ? sender.DisplayName 
                    : sender?.Email ?? "Someone";

                // Collect all device tokens from all members
                var allTokens = new List<string>();
                foreach (var memberId in memberUserIds)
                {
                    var tokens = await _deviceTokenRepository.GetTokensByUserId(memberId);
                    if (tokens != null && tokens.Any())
                    {
                        allTokens.AddRange(tokens);
                    }
                }

                if (allTokens.Any())
                {
                    // Prepare notification content based on message type
                    string notificationBody = request.MessageType switch
                    {
                        "IMAGE" => "Đã gửi một hình ảnh",
                        "FILE" => "Đã gửi một tệp",
                        "VIDEO" => "Đã gửi một video",
                        "AUDIO" => "Đã gửi một tin nhắn thoại",
                        "DATE_PLAN" => "Đã chia sẻ một kế hoạch hẹn hò",
                        _ => request.Content ?? "Đã gửi một tin nhắn"
                    };

                    // For group conversations, format: "SenderName in GroupName"
                    string notificationTitle = senderName;
                    if (isGroupConversation && !string.IsNullOrWhiteSpace(conversation?.Name))
                    {
                        notificationTitle = $"{conversation.Name} đã gửi tin nhắn";                    
                    }

                    var notificationRequest = new SendNotificationRequest
                    {
                        Title = notificationTitle,
                        Body = notificationBody,
                        ImageUrl = sender?.AvatarUrl,
                        Data = new Dictionary<string, string>
                        {
                            { "type", "CHAT" },
                            { "conversationId", request.ConversationId.ToString() },
                            { "messageId", message.Id.ToString() },
                            { "click_action", "FLUTTER_NOTIFICATION_CLICK" },
                            { "route", "/chat" }
                        }
                    };

                    // Send notification to all tokens
                    await _fcmService.SendMultiNotificationAsync(allTokens, notificationRequest);
                }
            }
            catch (Exception ex)
            {
                // Log error but don't fail the message send operation
                Console.WriteLine($"Failed to send push notification: {ex.Message}");
            }
        }

        // Return response for sender with IsMine = true
        var response = await MapToMessageResponseAsync(messageWithSender, currentUserId);
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

        // Pre-fetch all DatePlans for DATE_PLAN messages to avoid DbContext threading issues
        var datePlanIds = messages
            .Where(m => m.MessageType == "DATE_PLAN" && m.ReferenceId.HasValue)
            .Select(m => m.ReferenceId!.Value)
            .Distinct()
            .ToList();

        var datePlans = new Dictionary<int, DatePlan>();
        if (datePlanIds.Any())
        {
            var datePlanList = await _unitOfWork.Context.DatePlans
                .AsNoTracking()
                .Include(dp => dp.DatePlanItems.Where(dpi => dpi.IsDeleted == false).OrderBy(dpi => dpi.OrderIndex))
                    .ThenInclude(dpi => dpi.VenueLocation)
                .Where(dp => datePlanIds.Contains(dp.Id) && dp.IsDeleted == false)
                .ToListAsync(cancellationToken);

            datePlans = datePlanList.ToDictionary(dp => dp.Id);
        }

        // Map messages sequentially to avoid threading issues
        var messageResponses = new List<MessageResponse>();
        foreach (var message in messages)
        {
            messageResponses.Add(await MapToMessageResponseAsync(message, currentUserId, datePlans));
        }

        return new MessagesPageResponse
        {
            Messages = messageResponses,
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

        if (request.ConversationId <= 0 || request.MemberId <= 0)
            throw new Exception("Invalid conversation or member ID");

        // Check conversation exists and is group
        var conversation = await _conversationRepository.GetByIdAsync(request.ConversationId);
        if (conversation == null)
            throw new Exception("Conversation not found");

        if (conversation.Type != "GROUP")
            throw new Exception("Can only remove members from group conversations");

        // Check if current user is admin (unless removing self)
        if (currentUserId != request.MemberId)
        {
            var currentMember = await _memberRepository.GetMemberAsync(request.ConversationId, currentUserId, cancellationToken);
            if (currentMember?.Role != "ADMIN" || currentMember?.IsActive != true)
                throw new Exception("Only admins can remove members");
        }

        // Remove member
        var member = await _memberRepository.GetMemberAsync(request.ConversationId, request.MemberId, cancellationToken);
        if (member == null || member.IsActive == false)
            throw new Exception("Member not found");

        member.IsActive = false;
        _memberRepository.Update(member);
        await _unitOfWork.SaveChangesAsync();

         // Notify removed member via SignalR
        await _hubContext.Clients.User(request.MemberId.ToString())
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
            MemberId = currentUserId
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

        // // Only sender can delete
        // if (message.SenderId != currentUserId)
        //     throw new Exception("You can only delete your own messages");

        message.IsDeleted = true;
        message.UpdatedAt = DateTime.UtcNow;
        _messageRepository.Update(message);
        await _unitOfWork.SaveChangesAsync();

        if (message.ConversationId != null)
        {
            var members = await _memberRepository.GetActiveConversationMembersAsync(message.ConversationId.Value, cancellationToken);
            foreach (var member in members)
            {
                if (member.UserId == null)
                    continue;
                    
                await _hubContext.Clients.User(member.UserId.Value.ToString())
                    .SendAsync("MessageDeleted", new 
                    { 
                        messageId = messageId, 
                        conversationId = message.ConversationId.Value 
                    }, cancellationToken);
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
        
        // Pre-fetch all DatePlans for DATE_PLAN messages to avoid DbContext threading issues
        var datePlanIds = messages
            .Where(m => m.MessageType == "DATE_PLAN" && m.ReferenceId.HasValue)
            .Select(m => m.ReferenceId!.Value)
            .Distinct()
            .ToList();

        var datePlans = new Dictionary<int, DatePlan>();
        if (datePlanIds.Any())
        {
            var datePlanList = await _unitOfWork.Context.DatePlans
                .AsNoTracking()
                .Include(dp => dp.DatePlanItems.Where(dpi => dpi.IsDeleted == false).OrderBy(dpi => dpi.OrderIndex))
                    .ThenInclude(dpi => dpi.VenueLocation)
                .Where(dp => datePlanIds.Contains(dp.Id) && dp.IsDeleted == false)
                .ToListAsync(cancellationToken);

            datePlans = datePlanList.ToDictionary(dp => dp.Id);
        }

        // Map messages sequentially
        var messageResponses = new List<MessageResponse>();
        foreach (var message in messages)
        {
            messageResponses.Add(await MapToMessageResponseAsync(message, currentUserId, datePlans));
        }

        return messageResponses;
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
            response.LastMessage = await MapToMessageResponseAsync(lastMessage, currentUserId);
        }

        // Get unread count
        response.UnreadCount = await _memberRepository.GetUnreadCountAsync(conversation.Id, currentUserId, cancellationToken);

        if (conversation.Type == "DIRECT")
        {
            var otherUser = response.Members.FirstOrDefault(m => m.UserId != currentUserId);
            if (otherUser != null)
            {
                response.Name = otherUser.FullName;
            }
        }

        return response;
    }

    private async Task<MessageResponse> MapToMessageResponseAsync(
        Message message, 
        int currentUserId, 
        Dictionary<int, DatePlan>? datePlans = null)
    {
        var response = new MessageResponse
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
            CreatedAt = message.CreatedAt,
            UpdatedAt = message.UpdatedAt,
            IsMine = message.SenderId == currentUserId
        };

        // Parse metadata and populate appropriate fields based on MessageType
        // For DATE_PLAN messages, use pre-fetched data if available
        if (message.MessageType == "DATE_PLAN" && message.ReferenceId.HasValue)
        {
            DatePlan? datePlan = null;
            
            // Try to get from pre-fetched dictionary first
            if (datePlans != null && datePlans.TryGetValue(message.ReferenceId.Value, out var cachedPlan))
            {
                datePlan = cachedPlan;
            }
            else
            {
                // Fallback: query database (for single message operations like SendMessage)
                datePlan = await _unitOfWork.Context.DatePlans
                    .AsNoTracking()
                    .Include(dp => dp.DatePlanItems.Where(dpi => dpi.IsDeleted == false).OrderBy(dpi => dpi.OrderIndex))
                        .ThenInclude(dpi => dpi.VenueLocation)
                    .Where(dp => dp.Id == message.ReferenceId.Value && dp.IsDeleted == false)
                    .FirstOrDefaultAsync();
            }

            if (datePlan != null)
            {
                var firstVenue = datePlan.DatePlanItems?.FirstOrDefault()?.VenueLocation;
                var imageUrl = firstVenue?.CoverImage;
                if (!string.IsNullOrWhiteSpace(imageUrl) && imageUrl.Contains(','))
                {
                    imageUrl = imageUrl.Split(',')[0].Trim();
                }

                var datePlanInfo = new DatePlanInfoDto
                {
                    DatePlanId = datePlan.Id,
                    Title = datePlan.Title,
                    Status = datePlan.Status,
                    PlannedStartAt = datePlan.PlannedStartAt,
                    PlannedEndAt = datePlan.PlannedEndAt,
                    EstimatedBudget = datePlan.EstimatedBudget,
                    TotalCount = datePlan.DatePlanItems?.Count ?? 0,
                    ImageDatePlanUrl = imageUrl ?? "https://couplemood-store.s3.ap-southeast-2.amazonaws.com/images/46/e783d6de-d417-4bb7-88a4-9ddce30bd8bc.jpg"
                };

                response.DatePlanInfo = datePlanInfo;
                response.Metadata = new
                {
                    datePlanId = datePlan.Id,
                    title = datePlan.Title,
                    status = datePlan.Status,
                    plannedStartAt = datePlan.PlannedStartAt,
                    plannedEndAt = datePlan.PlannedEndAt,
                    estimatedBudget = datePlan.EstimatedBudget,
                    totalCount = datePlan.DatePlanItems?.Count ?? 0,
                    imageDatePlanUrl = datePlanInfo.ImageDatePlanUrl
                };
            }
        }
        else if (!string.IsNullOrWhiteSpace(message.Metadata))
        {
            try
            {
                // Parse based on message type for better reliability
                if (message.MessageType == "FILE" || message.MessageType == "IMAGE" || 
                    message.MessageType == "VIDEO" || message.MessageType == "AUDIO")
                {
                    // Parse as file metadata
                    var fileInfo = System.Text.Json.JsonSerializer.Deserialize<FileMetadata>(message.Metadata);
                    if (fileInfo != null && !string.IsNullOrWhiteSpace(fileInfo.FileUrl))
                    {
                        response.FileUrl = fileInfo.FileUrl;
                        response.FileName = fileInfo.FileName;
                        response.FileSize = fileInfo.FileSize;
                        response.Metadata = fileInfo;
                    }
                }
                else
                {
                    // For other message types, try to parse as generic object
                    response.Metadata = System.Text.Json.JsonSerializer.Deserialize<object>(message.Metadata);
                }
            }
            catch
            {
                // If parse fails, keep metadata as raw string in object form
                try
                {
                    response.Metadata = System.Text.Json.JsonSerializer.Deserialize<object>(message.Metadata);
                }
                catch
                {
                    response.Metadata = message.Metadata;
                }
            }
        }

        return response;
    }

    private class FileMetadata
    {
        [System.Text.Json.Serialization.JsonPropertyName("FileUrl")]
        public string? FileUrl { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("FileName")]
        public string? FileName { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("FileSize")]
        public long? FileSize { get; set; }
    }

    private class DatePlanMetadata
    {
        [System.Text.Json.Serialization.JsonPropertyName("datePlanId")]
        public int DatePlanId { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("title")]
        public string? Title { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("status")]
        public string? Status { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("plannedStartAt")]
        public DateTime? PlannedStartAt { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("plannedEndAt")]
        public DateTime? PlannedEndAt { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("estimatedBudget")]
        public decimal? EstimatedBudget { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("totalCount")]
        public int TotalCount { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("imageDatePlanUrl")]
        public string? ImageDatePlanUrl { get; set; }
    }
}
