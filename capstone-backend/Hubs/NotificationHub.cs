using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace capstone_backend.Hubs
{
    [Authorize]
    public class NotificationHub : Hub
    {
        public override async Task OnConnectedAsync()
        {
            // 1. Get UserId
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            // 2. Add to group based on UserId (for both moble and website)
            if (!string.IsNullOrEmpty(userId))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"User_{userId}");

                // Log
                Console.WriteLine($"--> User {userId} connected with ID: {Context.ConnectionId}");
            }

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            // 1. Get UserId
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            // 2. Remove from group based on UserId
            if (!string.IsNullOrEmpty(userId))
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"User_{userId}");
                Console.WriteLine($"--> User {userId} disconnected");
            }
            await base.OnDisconnectedAsync(exception);
        }
    }
}
