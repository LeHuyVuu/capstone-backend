# Messaging System Implementation

## 📋 Overview
Complete real-time messaging system với SignalR, hỗ trợ:
- ✅ Chat 1-1 giữa users
- ✅ Group chat với nhiều members
- ✅ Typing indicators (real-time)
- ✅ Online/Offline status
- ✅ Read receipts
- ✅ Custom messages (Date Plan, Location, Events)
- ✅ Message search
- ✅ Comprehensive validation & error handling

## 🗄️ Database Schema

### Tables
1. **conversations** - Lưu cuộc hội thoại
2. **conversation_members** - Thành viên trong hội thoại
3. **messages** - Tin nhắn

### Migration Script
```sql
-- Execute migration script tại: 
c:\Users\Dell\.dbclient\storage\...\public.sql
```

## 🏗️ Architecture

```
Client (Mobile/Web)
    ↕️ WebSocket (SignalR)
MessagingHub
    ↕️
MessagingService (Business Logic)
    ↕️
Repositories
    ↕️
PostgreSQL Database
```

## 📁 Files Created

### Entities
- `Data/Entities/Conversation.cs`
- `Data/Entities/ConversationMember.cs`
- `Data/Entities/Message.cs`

### Enums
- `Data/Enums/ConversationType.cs`
- `Data/Enums/MemberRole.cs`
- `Data/Enums/MessageType.cs`

### Repositories
- `Data/Interfaces/IConversationRepository.cs`
- `Data/Interfaces/IConversationMemberRepository.cs`
- `Data/Interfaces/IMessageRepository.cs`
- `Data/Repositories/ConversationRepository.cs`
- `Data/Repositories/ConversationMemberRepository.cs`
- `Data/Repositories/MessageRepository.cs`

### DTOs
- `Business/DTOs/Messaging/MessagingRequests.cs`
- `Business/DTOs/Messaging/MessagingResponses.cs`

### Services
- `Business/Interfaces/IMessagingService.cs`
- `Business/Services/MessagingService.cs`

### SignalR Hub
- `Hubs/MessagingHub.cs`

### Controllers
- `Api/Controllers/MessagingController.cs`

### Updated Files
- `Data/Context/MyDbContext.cs` - Added messaging entities
- `Extensions/ServiceExtensions.cs` - Registered services
- `Program.cs` - Added MessagingHub mapping

## 🚀 API Endpoints

### Conversations
```http
POST   /api/messaging/conversations
POST   /api/messaging/conversations/direct/{otherUserId}
GET    /api/messaging/conversations
GET    /api/messaging/conversations/{conversationId}
POST   /api/messaging/conversations/{conversationId}/members
DELETE /api/messaging/conversations/{conversationId}/members/{memberId}
POST   /api/messaging/conversations/{conversationId}/leave
```

### Messages
```http
POST   /api/messaging/messages
GET    /api/messaging/conversations/{conversationId}/messages
POST   /api/messaging/messages/read
DELETE /api/messaging/messages/{messageId}
GET    /api/messaging/conversations/{conversationId}/messages/search
```

## 🔌 SignalR Hub Events

### Client → Server
```javascript
// Join conversation room
await connection.invoke("JoinConversation", conversationId);

// Leave conversation room
await connection.invoke("LeaveConversation", conversationId);

// Send typing indicator
await connection.invoke("SendTypingIndicator", conversationId, true);

// Get online status
var onlineUsers = await connection.invoke("GetOnlineStatus", [userId1, userId2]);
```

### Server → Client
```javascript
// Receive new message
connection.on("ReceiveMessage", (message) => {
    console.log("New message:", message);
});

// User typing
connection.on("UserTyping", (typingInfo) => {
    console.log(`${typingInfo.username} is typing...`);
});

// User online/offline
connection.on("UserOnline", (userId) => {
    console.log(`User ${userId} is online`);
});

connection.on("UserOffline", (userId, lastSeen) => {
    console.log(`User ${userId} went offline at ${lastSeen}`);
});

// Message read
connection.on("MessageRead", (conversationId, messageId, userId) => {
    console.log(`Message ${messageId} read by user ${userId}`);
});

// New conversation
connection.on("NewConversation", (conversationId) => {
    console.log(`Added to conversation ${conversationId}`);
});

// Message deleted
connection.on("MessageDeleted", (messageId) => {
    console.log(`Message ${messageId} was deleted`);
});
```

## 💡 Usage Examples

### 1. Create Direct Conversation
```http
POST /api/messaging/conversations/direct/123
Authorization: Bearer {token}

Response:
{
  "id": 1,
  "type": "DIRECT",
  "members": [...],
  "unreadCount": 0
}
```

### 2. Send Text Message
```http
POST /api/messaging/messages
Authorization: Bearer {token}

{
  "conversationId": 1,
  "content": "Hello!",
  "messageType": "TEXT"
}
```

### 3. Send Date Plan Card
```http
POST /api/messaging/messages
Authorization: Bearer {token}

{
  "conversationId": 1,
  "content": "Check out this date plan!",
  "messageType": "DATE_PLAN",
  "referenceId": 456,
  "referenceType": "DatePlan",
  "metadata": "{\"title\":\"Dinner Date\",\"date\":\"2026-02-10\"}"
}
```

### 4. Get Messages with Pagination
```http
GET /api/messaging/conversations/1/messages?pageNumber=1&pageSize=50
Authorization: Bearer {token}
```

### 5. Mark as Read
```http
POST /api/messaging/messages/read
Authorization: Bearer {token}

{
  "conversationId": 1,
  "messageId": 123
}
```

## 🛡️ Security & Validation

### Authentication
- All endpoints require JWT Bearer token
- SignalR hub requires authentication

### Authorization
- Users can only access conversations they are members of
- Only admins can add/remove members in groups
- Users can only delete their own messages

### Validation
✅ Validate conversation exists
✅ Validate user is member
✅ Validate message content
✅ Prevent duplicate direct conversations
✅ Sanitize pagination parameters
✅ Limit page size to prevent abuse
✅ Validate group operations
✅ Handle edge cases (null, empty, invalid IDs)

## 🎯 Edge Cases Handled

1. **Duplicate Conversations**
   - Check existing direct conversation before creating new one
   
2. **Concurrent Connections**
   - Track multiple connections per user
   - User offline only when all connections close

3. **Typing Indicators**
   - Auto-clear after 3 seconds
   - No database writes (memory only)

4. **Empty/Null Values**
   - All nullable fields in database
   - Comprehensive null checks in code

5. **Pagination Limits**
   - Max page size: 100
   - Search results limited to 50

6. **Member Management**
   - Soft delete for members (keep history)
   - Prevent removing self from direct chat
   - Only admins can modify groups

7. **Message Validation**
   - TEXT: content required
   - Other types: referenceId required
   - Sender must be active member

## 🚀 Next Steps

1. Run database migration script
2. Build project: `dotnet build`
3. Test endpoints with Swagger
4. Implement frontend SignalR client
5. Add notifications for new messages
6. Implement message attachments (images, files)
7. Add message reactions/emoji
8. Implement message threading/replies

## 🔧 Configuration

### appsettings.json
No additional configuration needed. Uses existing:
- Database connection
- JWT authentication
- SignalR (already configured)

### Environment Variables
Uses existing PostgreSQL environment variables.

## 📊 Performance Optimizations

- ✅ Database indexes on foreign keys
- ✅ Pagination prevents large queries
- ✅ Eager loading with Include()
- ✅ SignalR groups for targeted messages
- ✅ In-memory tracking for online status
- ✅ Read receipts use last_read_message_id (efficient)

## 🐛 Troubleshooting

### Build Errors
```bash
dotnet build
# Check for missing references
```

### Database Errors
```sql
-- Verify tables created
SELECT * FROM conversations;
SELECT * FROM conversation_members;
SELECT * FROM messages;
```

### SignalR Connection Issues
- Verify JWT token is valid
- Check CORS configuration
- Ensure WebSocket is enabled

## 📝 Notes

- DateTime stored as UTC (no timezone issues)
- TEXT datatype for PostgreSQL compatibility
- Enum stored as integer in database
- All fields nullable to prevent save errors
- Comprehensive error messages for debugging
