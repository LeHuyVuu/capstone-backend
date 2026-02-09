using capstone_backend.Api.Controllers;
using capstone_backend.Business.DTOs.Messaging;
using capstone_backend.Business.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace capstone_backend.Controllers;

/// <summary>
/// Controller for messaging operations
/// </summary>
[Authorize]
[Route("api/[controller]")]
public class MessagingController : BaseController
{
    private readonly IMessagingService _messagingService;

    public MessagingController(IMessagingService messagingService)
    {
        _messagingService = messagingService;
    }

    /// <summary>
    /// Create a new conversation
    /// </summary>
    [HttpPost("conversations")]
    [ProducesResponseType(typeof(ConversationResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CreateConversation(
        [FromBody] CreateConversationRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var userId = GetCurrentUserId() ?? 0;
        var result = await _messagingService.CreateConversationAsync(userId, request, cancellationToken);
        return CreatedAtAction(nameof(GetConversation), new { conversationId = result.Id }, result);
    }

    /// <summary>
    /// Get or create direct conversation with another user
    /// </summary>
    [HttpPost("conversations/direct/{otherUserId}")]
    [ProducesResponseType(typeof(ConversationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetOrCreateDirectConversation(
        [FromRoute] int otherUserId,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId() ?? 0;
        var result = await _messagingService.GetOrCreateDirectConversationAsync(userId, otherUserId, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Get all conversations for current user
    /// </summary>
    [HttpGet("conversations")]
    [ProducesResponseType(typeof(List<ConversationResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetConversations(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId() ?? 0;
        var result = await _messagingService.GetUserConversationsAsync(userId, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Get conversation by ID
    /// </summary>
    [HttpGet("conversations/{conversationId}")]
    [ProducesResponseType(typeof(ConversationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetConversation(
        [FromRoute] int conversationId,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId() ?? 0;
        var result = await _messagingService.GetConversationByIdAsync(userId, conversationId, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Send a message
    /// </summary>
    [HttpPost("messages")]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> SendMessage(
        [FromBody] SendMessageRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var userId = GetCurrentUserId() ?? 0;
        var result = await _messagingService.SendMessageAsync(userId, request, cancellationToken);
        return CreatedAtAction(nameof(GetMessages), new { conversationId = request.ConversationId }, result);
    }

    /// <summary>
    /// Get messages in a conversation
    /// </summary>
    [HttpGet("conversations/{conversationId}/messages")]
    [ProducesResponseType(typeof(MessagesPageResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMessages(
        [FromRoute] int conversationId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId() ?? 0;
        var result = await _messagingService.GetMessagesAsync(userId, conversationId, pageNumber, pageSize, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Mark message as read
    /// </summary>
    [HttpPost("messages/read")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> MarkAsRead(
        [FromBody] MarkAsReadRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var userId = GetCurrentUserId() ?? 0;
        await _messagingService.MarkAsReadAsync(userId, request, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Add members to group conversation
    /// </summary>
    [HttpPost("conversations/{conversationId}/members")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddMembers(
        [FromRoute] int conversationId,
        [FromBody] List<int> memberIds,
        CancellationToken cancellationToken)
    {
        if (memberIds == null || !memberIds.Any())
            return BadRequest("At least one member is required");

        var userId = GetCurrentUserId() ?? 0;
        await _messagingService.AddMembersAsync(userId, new AddMembersRequest
        {
            ConversationId = conversationId,
            MemberIds = memberIds
        }, cancellationToken);
        
        return NoContent();
    }

    /// <summary>
    /// Remove member from group conversation
    /// </summary>
    [HttpDelete("conversations/{conversationId}/members/{memberId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveMember(
        [FromRoute] int conversationId,
        [FromRoute] int memberId,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId() ?? 0;
        await _messagingService.RemoveMemberAsync(userId, new RemoveMemberRequest
        {
            ConversationId = conversationId,
            UserId = memberId
        }, cancellationToken);
        
        return NoContent();
    }

    /// <summary>
    /// Leave conversation
    /// </summary>
    [HttpPost("conversations/{conversationId}/leave")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> LeaveConversation(
        [FromRoute] int conversationId,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId() ?? 0;
        await _messagingService.LeaveConversationAsync(userId, conversationId, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Delete message (soft delete)
    /// </summary>
    [HttpDelete("messages/{messageId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteMessage(
        [FromRoute] int messageId,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId() ?? 0;
        await _messagingService.DeleteMessageAsync(userId, messageId, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Search messages in conversation
    /// </summary>
    [HttpGet("conversations/{conversationId}/messages/search")]
    [ProducesResponseType(typeof(List<MessageResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> SearchMessages(
        [FromRoute] int conversationId,
        [FromQuery] string searchTerm,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
            return BadRequest("Search term is required");

        var userId = GetCurrentUserId() ?? 0;
        var result = await _messagingService.SearchMessagesAsync(userId, conversationId, searchTerm, cancellationToken);
        return Ok(result);
    }

}
