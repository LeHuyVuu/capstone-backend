# 📱 Messaging System API Documentation

## 📋 Table of Contents
1. [Overview](#overview)
2. [Authentication](#authentication)
3. [REST API Endpoints](#rest-api-endpoints)
4. [SignalR Real-time Hub](#signalr-real-time-hub)
5. [Mobile Integration](#mobile-integration)
6. [Data Models](#data-models)
7. [Flow Examples](#flow-examples)
8. [Error Handling](#error-handling)
9. [Best Practices](#best-practices)

---

## 🎯 Overview

Hệ thống messaging hỗ trợ:
- ✅ Chat 1-1 (Direct conversation)
- ✅ Group chat
- ✅ Real-time messaging với SignalR
- ✅ Typing indicators
- ✅ Online/Offline status
- ✅ Read receipts
- ✅ Message search
- ✅ Rich messages (Date Plan, Location, etc.)

**Architecture:**
```
Mobile App
    ├─ REST API (HTTP) → Gửi tin, tạo conversation, load history
    └─ SignalR (WebSocket) → Nhận tin real-time, typing, online status
```

---

## 🔐 Authentication

### Tất cả API đều yêu cầu JWT Token

**Header required:**
```
Authorization: Bearer {your_jwt_token}
```

**Cách lấy token:**
1. Login qua `/api/auth/login`
2. Nhận `accessToken` từ response
3. Lưu token vào local storage
4. Gửi token trong header cho mọi request

**Example:**
```http
GET /api/messaging/conversations
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

---

## 📡 REST API Endpoints

Base URL: `https://your-api-domain.com/api/messaging`

### 1. Conversations

#### 1.1. Create Group Conversation
**Tạo group chat mới với nhiều thành viên**

```http
POST /api/messaging/conversations
Authorization: Bearer {token}
Content-Type: application/json
```

**Request Body:**
```json
{
  "type": "GROUP",
  "name": "Family Group",
  "memberIds": [2, 3, 4, 5]
}
```

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| type | string | ✅ Yes | `"DIRECT"` hoặc `"GROUP"` |
| name | string | Chỉ với GROUP | Tên group chat |
| memberIds | array[int] | ✅ Yes | Danh sách User ID muốn thêm vào |

**Response: 201 Created**
```json
{
  "id": 1,
  "type": "GROUP",
  "name": "Family Group",
  "createdBy": 1,
  "createdAt": "2026-02-07T10:30:00Z",
  "members": [
    {
      "userId": 1,
      "username": "user1@email.com",
      "fullName": "Nguyen Van A",
      "avatar": null,
      "role": "ADMIN",
      "joinedAt": "2026-02-07T10:30:00Z",
      "isOnline": true
    },
    {
      "userId": 2,
      "username": "user2@email.com",
      "fullName": "Tran Thi B",
      "avatar": null,
      "role": "MEMBER",
      "joinedAt": "2026-02-07T10:30:00Z",
      "isOnline": false
    }
  ],
  "lastMessage": null,
  "unreadCount": 0
}
```

**Errors:**
- `400 Bad Request`: Thiếu field required hoặc validation fail
- `401 Unauthorized`: Token không hợp lệ
- `403 Forbidden`: Không có quyền

**Mobile Implementation:**
- Dùng để tạo group chat mới
- Hiển thị form nhập tên group, chọn members
- Sau khi tạo xong, navigate đến chat screen

---

#### 1.2. Get or Create Direct Conversation
**Tạo hoặc lấy conversation 1-1 với user khác**

```http
POST /api/messaging/conversations/direct/{otherUserId}
Authorization: Bearer {token}
```

**Path Parameters:**
| Parameter | Type | Description |
|-----------|------|-------------|
| otherUserId | int | ID của user muốn chat |

**Request:** Không cần body

**Response: 200 OK**
```json
{
  "id": 2,
  "type": "DIRECT",
  "name": null,
  "createdBy": 1,
  "createdAt": "2026-02-07T09:00:00Z",
  "members": [
    {
      "userId": 1,
      "username": "user1@email.com",
      "fullName": "Nguyen Van A",
      "avatar": null,
      "role": "ADMIN",
      "joinedAt": "2026-02-07T09:00:00Z",
      "isOnline": true
    },
    {
      "userId": 5,
      "username": "user5@email.com",
      "fullName": "Le Van E",
      "avatar": null,
      "role": "MEMBER",
      "joinedAt": "2026-02-07T09:00:00Z",
      "isOnline": false
    }
  ],
  "lastMessage": {
    "id": 123,
    "conversationId": 2,
    "senderId": 5,
    "senderName": "user5@email.com",
    "senderAvatar": null,
    "content": "Hi there!",
    "messageType": "TEXT",
    "referenceId": null,
    "referenceType": null,
    "metadata": null,
    "createdAt": "2026-02-07T10:25:00Z",
    "updatedAt": null,
    "isMine": false
  },
  "unreadCount": 3
}
```

**Behavior:**
- Nếu conversation đã tồn tại → Trả về conversation đó
- Nếu chưa có → Tạo mới và trả về

**Mobile Implementation:**
- Dùng khi user click vào profile của user khác và nhấn "Message"
- Không cần check xem đã có conversation chưa, API tự handle
- Navigate đến chat screen với conversationId nhận được

---

#### 1.3. Get All Conversations
**Lấy danh sách tất cả conversations của user**

```http
GET /api/messaging/conversations
Authorization: Bearer {token}
```

**Request:** Không cần parameters

**Response: 200 OK**
```json
[
  {
    "id": 1,
    "type": "GROUP",
    "name": "Family Group",
    "createdBy": 1,
    "createdAt": "2026-02-07T10:30:00Z",
    "members": [...],
    "lastMessage": {
      "id": 456,
      "content": "See you tomorrow!",
      "messageType": "TEXT",
      "createdAt": "2026-02-07T15:20:00Z",
      "isMine": true
    },
    "unreadCount": 0
  },
  {
    "id": 2,
    "type": "DIRECT",
    "name": null,
    "createdBy": 1,
    "createdAt": "2026-02-07T09:00:00Z",
    "members": [...],
    "lastMessage": {
      "id": 123,
      "content": "Hi there!",
      "messageType": "TEXT",
      "createdAt": "2026-02-07T10:25:00Z",
      "isMine": false
    },
    "unreadCount": 3
  }
]
```

**Sorting:** Tự động sort theo tin nhắn mới nhất (lastMessage.createdAt)

**Mobile Implementation:**
- Hiển thị ở màn hình danh sách chat (Chat List Screen)
- Mỗi item hiển thị:
  - Avatar (avatar của user khác nếu DIRECT, hoặc group icon nếu GROUP)
  - Tên (fullName của user khác hoặc group name)
  - Last message preview
  - Unread count badge (nếu > 0)
  - Timestamp
- Pull-to-refresh để load lại
- Navigate đến chat screen khi click vào item

---

#### 1.4. Get Conversation by ID
**Lấy chi tiết 1 conversation**

```http
GET /api/messaging/conversations/{conversationId}
Authorization: Bearer {token}
```

**Path Parameters:**
| Parameter | Type | Description |
|-----------|------|-------------|
| conversationId | int | ID của conversation |

**Response: 200 OK**
```json
{
  "id": 1,
  "type": "GROUP",
  "name": "Family Group",
  "createdBy": 1,
  "createdAt": "2026-02-07T10:30:00Z",
  "members": [
    {
      "userId": 1,
      "username": "user1@email.com",
      "fullName": "Nguyen Van A",
      "avatar": null,
      "role": "ADMIN",
      "joinedAt": "2026-02-07T10:30:00Z",
      "isOnline": true
    },
    {
      "userId": 2,
      "username": "user2@email.com",
      "fullName": "Tran Thi B",
      "avatar": null,
      "role": "MEMBER",
      "joinedAt": "2026-02-07T10:30:00Z",
      "isOnline": false
    }
  ],
  "lastMessage": {...},
  "unreadCount": 0
}
```

**Errors:**
- `403 Forbidden`: User không phải member của conversation
- `404 Not Found`: Conversation không tồn tại

**Mobile Implementation:**
- Dùng để load thông tin conversation khi vào chat screen
- Hiển thị members, admin, online status

---

#### 1.5. Add Members to Group
**Thêm thành viên vào group chat (chỉ ADMIN)**

```http
POST /api/messaging/conversations/{conversationId}/members
Authorization: Bearer {token}
Content-Type: application/json
```

**Request Body:**
```json
{
  "conversationId": 1,
  "memberIds": [6, 7, 8]
}
```

**Response: 200 OK**
```json
{
  "message": "Members added successfully"
}
```

**Errors:**
- `400 Bad Request`: Không phải GROUP conversation
- `403 Forbidden`: User không phải ADMIN

**Mobile Implementation:**
- Chỉ hiển thị button "Add Members" nếu user.role = "ADMIN"
- Show member picker dialog
- Sau khi add xong, reload conversation details

---

#### 1.6. Remove Member from Group
**Xóa thành viên khỏi group (chỉ ADMIN)**

```http
DELETE /api/messaging/conversations/{conversationId}/members/{userId}
Authorization: Bearer {token}
```

**Response: 200 OK**

**Mobile Implementation:**
- Chỉ ADMIN mới thấy option "Remove" trên member
- Có thể remove chính mình (Leave group)

---

#### 1.7. Leave Group
**Rời khỏi group chat**

```http
POST /api/messaging/conversations/{conversationId}/leave
Authorization: Bearer {token}
```

**Response: 200 OK**

**Mobile Implementation:**
- Button "Leave Group" trong group settings
- Sau khi leave, quay về chat list

---

### 2. Messages

#### 2.1. Send Message
**Gửi tin nhắn vào conversation**

```http
POST /api/messaging/messages
Authorization: Bearer {token}
Content-Type: application/json
```

**Request Body - Text Message:**
```json
{
  "conversationId": 1,
  "content": "Hello everyone!",
  "messageType": "TEXT"
}
```

**Request Body - Rich Message (Date Plan):**
```json
{
  "conversationId": 1,
  "content": "Check out this date plan!",
  "messageType": "DATE_PLAN",
  "referenceId": 456,
  "referenceType": "DatePlan",
  "metadata": "{\"title\":\"Romantic Dinner\",\"date\":\"2026-02-14\",\"location\":\"Italian Restaurant\"}"
}
```

**Request Fields:**
| Field | Type | Required | Description |
|-------|------|----------|-------------|
| conversationId | int | ✅ Yes | ID của conversation |
| content | string | For TEXT | Nội dung tin nhắn |
| messageType | string | ✅ Yes | `TEXT`, `IMAGE`, `FILE`, `DATE_PLAN`, `LOCATION`, `EVENT`, `POLL`, `VOICE` |
| referenceId | int | For rich messages | ID của object được share (DatePlan ID, Location ID...) |
| referenceType | string | For rich messages | Loại object: `DatePlan`, `Location`, `Event`... |
| metadata | string | Optional | JSON string chứa thông tin bổ sung |

**Response: 201 Created**
```json
{
  "id": 789,
  "conversationId": 1,
  "senderId": 1,
  "senderName": "user1@email.com",
  "senderAvatar": null,
  "content": "Hello everyone!",
  "messageType": "TEXT",
  "referenceId": null,
  "referenceType": null,
  "metadata": null,
  "createdAt": "2026-02-07T16:45:30Z",
  "updatedAt": null,
  "isMine": true
}
```

**Behavior:**
- Server tự động broadcast tin nhắn qua SignalR đến tất cả members
- Sender cũng nhận tin qua SignalR để đồng bộ multi-device

**Mobile Implementation:**

**For TEXT:**
1. User nhập text vào TextField
2. Click Send button
3. POST message lên server
4. Hiển thị tin ở trạng thái "sending" (optional)
5. Khi nhận response (hoặc qua SignalR), update status thành "sent"

**For Rich Messages:**
1. User chọn Date Plan từ danh sách
2. Hiển thị preview card
3. Click "Share to chat"
4. POST message với messageType = "DATE_PLAN" + referenceId
5. Mobile parse metadata để hiển thị card đẹp

**Message Types Implementation:**
- `TEXT`: Bubble text bình thường
- `IMAGE`: Hiển thị image preview, click để zoom
- `FILE`: File icon + tên file, click để download
- `DATE_PLAN`: Card hiển thị title, date, location + button "View Details"
- `LOCATION`: Map preview + địa chỉ, click để mở Google Maps
- `VOICE`: Audio player với play button + waveform
- `EVENT`: Event card với thông tin event
- `POLL`: Poll UI với options để vote

---

#### 2.2. Get Messages (Pagination)
**Load tin nhắn của conversation với phân trang**

```http
GET /api/messaging/conversations/{conversationId}/messages?pageNumber=1&pageSize=50
Authorization: Bearer {token}
```

**Query Parameters:**
| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| pageNumber | int | 1 | Trang thứ mấy (1-based) |
| pageSize | int | 50 | Số tin mỗi trang (max: 100) |

**Response: 200 OK**
```json
{
  "messages": [
    {
      "id": 789,
      "conversationId": 1,
      "senderId": 1,
      "senderName": "user1@email.com",
      "senderAvatar": null,
      "content": "Hello!",
      "messageType": "TEXT",
      "referenceId": null,
      "referenceType": null,
      "metadata": null,
      "createdAt": "2026-02-07T16:45:30Z",
      "updatedAt": null,
      "isMine": true
    },
    {
      "id": 788,
      "conversationId": 1,
      "senderId": 2,
      "senderName": "user2@email.com",
      "senderAvatar": null,
      "content": "Hi there!",
      "messageType": "TEXT",
      "referenceId": null,
      "referenceType": null,
      "metadata": null,
      "createdAt": "2026-02-07T16:40:00Z",
      "updatedAt": null,
      "isMine": false
    }
  ],
  "pageNumber": 1,
  "pageSize": 50,
  "totalPages": 0,
  "hasNextPage": false
}
```

**Sorting:** Tin mới nhất ở đầu array (DESC by createdAt)

**Mobile Implementation:**
- Load page 1 khi vào chat screen
- Implement infinite scroll/pagination:
  - Khi scroll đến top → Load page tiếp theo
  - Check `hasNextPage` để biết còn page nào không
- Reverse list để hiển thị (tin mới nhất ở dưới cùng)
- Cache messages locally để tránh load lại nhiều lần

---

#### 2.3. Mark as Read
**Đánh dấu đã đọc tin nhắn**

```http
POST /api/messaging/messages/read
Authorization: Bearer {token}
Content-Type: application/json
```

**Request Body:**
```json
{
  "conversationId": 1,
  "messageId": 789
}
```

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| conversationId | int | ✅ Yes | ID của conversation |
| messageId | int | ✅ Yes | ID của tin nhắn cuối cùng đã đọc |

**Response: 200 OK**
```json
{
  "message": "Marked as read"
}
```

**Behavior:**
- Server lưu lastReadMessageId cho user
- Server broadcast "MessageRead" event qua SignalR đến sender
- Tất cả tin có id ≤ messageId được coi là đã đọc

**Mobile Implementation:**
- Tự động gọi API khi:
  - User vào chat screen
  - User scroll đến tin nhắn mới nhất
  - App quay về foreground khi đang ở chat screen
- Gọi với messageId = ID của tin mới nhất visible
- Update unread count badge sau khi mark read

---

#### 2.4. Delete Message
**Xóa tin nhắn (chỉ người gửi)**

```http
DELETE /api/messaging/messages/{messageId}
Authorization: Bearer {token}
```

**Response: 200 OK**

**Behavior:**
- Soft delete (is_deleted = true)
- Broadcast "MessageDeleted" qua SignalR

**Mobile Implementation:**
- Long press message → Show menu "Delete"
- Chỉ hiển thị option này nếu message.isMine = true
- Sau khi delete, hide message hoặc show "Message deleted"

---

#### 2.5. Search Messages
**Tìm kiếm tin nhắn trong conversation**

```http
GET /api/messaging/conversations/{conversationId}/messages/search?searchTerm=hello
Authorization: Bearer {token}
```

**Query Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| searchTerm | string | ✅ Yes | Từ khóa tìm kiếm |

**Response: 200 OK**
```json
[
  {
    "id": 123,
    "content": "Hello world!",
    "messageType": "TEXT",
    "senderId": 2,
    "createdAt": "2026-02-07T10:00:00Z",
    "isMine": false
  },
  {
    "id": 456,
    "content": "Hello everyone!",
    "messageType": "TEXT",
    "senderId": 1,
    "createdAt": "2026-02-07T16:45:00Z",
    "isMine": true
  }
]
```

**Mobile Implementation:**
- Search icon trong chat screen app bar
- Hiển thị search TextField
- Debounce input (wait 500ms after user stop typing)
- Highlight search term trong kết quả
- Click vào kết quả → Scroll đến tin nhắn đó trong chat

---

## 🔄 SignalR Real-time Hub

### Connection URL
```
wss://your-api-domain.com/hubs/messaging
```

### Package Requirements

**Flutter:**
```yaml
# pubspec.yaml
dependencies:
  signalr_netcore: ^1.3.7  # Recommended - stable, well maintained
```

**React Native:**
```bash
npm install @microsoft/signalr@7.0.0
```

**iOS Native:**
```swift
// Package.swift hoặc Podfile
.package(url: "https://github.com/moozzyk/SignalR-Client-Swift", from: "0.9.0")
```

**Android Native:**
```gradle
// build.gradle
implementation 'com.microsoft.signalr:signalr:7.0.0'
```

### Connection Setup

**1. Create Connection:**
```javascript
// Example (pseudo-code cho mọi platform)
connection = HubConnectionBuilder()
    .withUrl("wss://your-api.com/hubs/messaging", {
        accessTokenFactory: () => getUserToken()
    })
    .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
    .build();
```

**2. Setup Event Handlers (BEFORE start):**

Phải đăng ký listeners TRƯỚC khi gọi `start()`

**3. Start Connection:**
```javascript
await connection.start();
```

**4. Connection State:**
- `Disconnected` → Chưa kết nối
- `Connecting` → Đang kết nối
- `Connected` → Đã kết nối (có thể gửi/nhận)
- `Reconnecting` → Đang reconnect

### Server → Client Events (Receive)

Client cần đăng ký handlers cho các events này:

#### Event 1: `ReceiveMessage`
**Nhận tin nhắn mới real-time**

```javascript
connection.on("ReceiveMessage", (message) => {
  // message: MessageResponse object
  // {
  //   id: 789,
  //   conversationId: 1,
  //   senderId: 2,
  //   content: "Hello!",
  //   messageType: "TEXT",
  //   createdAt: "2026-02-07T16:45:30Z",
  //   isMine: false
  // }
  
  // Implementation:
  // 1. Check if đang ở chat screen của conversation này
  if (currentConversationId === message.conversationId) {
    // Add message vào list
    addMessageToChat(message);
    // Mark as read
    markAsRead(message.conversationId, message.id);
  } else {
    // Tăng unread count cho conversation đó
    incrementUnreadCount(message.conversationId);
    // Show notification
    showNotification(message);
  }
  
  // 2. Update lastMessage trong conversation list
  updateConversationLastMessage(message.conversationId, message);
  
  // 3. Play sound (optional)
  playNotificationSound();
});
```

#### Event 2: `UserTyping`
**Nhận typing indicator**

```javascript
connection.on("UserTyping", (typingInfo) => {
  // typingInfo:
  // {
  //   conversationId: 1,
  //   userId: 2,
  //   username: "user2@email.com",
  //   isTyping: true
  // }
  
  // Implementation:
  if (currentConversationId === typingInfo.conversationId) {
    if (typingInfo.isTyping) {
      showTypingIndicator(typingInfo.username);
      // Display: "user2@email.com is typing..."
    } else {
      hideTypingIndicator(typingInfo.userId);
    }
  }
});
```

#### Event 3: `UserOnline`
**User vừa online**

```javascript
connection.on("UserOnline", (userId) => {
  // userId: int
  
  // Implementation:
  // Update online status trong conversation members
  updateUserOnlineStatus(userId, true);
  
  // Update avatar với green dot indicator
  // Update last seen: "Online"
});
```

#### Event 4: `UserOffline`
**User vừa offline**

```javascript
connection.on("UserOffline", (userId, lastSeen) => {
  // userId: int
  // lastSeen: "2026-02-07T17:30:00Z"
  
  // Implementation:
  updateUserOnlineStatus(userId, false);
  updateUserLastSeen(userId, lastSeen);
  // Display: "Last seen 5 minutes ago"
});
```

#### Event 5: `MessageRead`
**Tin nhắn đã được đọc**

```javascript
connection.on("MessageRead", (conversationId, messageId, userId) => {
  // conversationId: int
  // messageId: int - Tin nhắn cuối cùng được đọc
  // userId: int - User đã đọc
  
  // Implementation (chỉ cho sender):
  if (currentConversationId === conversationId) {
    // Update read receipt UI
    // Show "Seen" hoặc avatar của user đã đọc
    markMessagesAsRead(conversationId, messageId, userId);
  }
});
```

#### Event 6: `MessageDeleted`
**Tin nhắn đã bị xóa**

```javascript
connection.on("MessageDeleted", (messageId) => {
  // messageId: int
  
  // Implementation:
  // Remove message khỏi UI hoặc replace bằng "Message deleted"
  removeMessageFromUI(messageId);
});
```

#### Event 7: `NewConversation`
**Được thêm vào conversation mới**

```javascript
connection.on("NewConversation", (conversationId) => {
  // conversationId: int
  
  // Implementation:
  // Load conversation details
  const conversation = await fetchConversation(conversationId);
  // Add vào conversation list
  addConversationToList(conversation);
  // Show notification: "You were added to 'Family Group'"
});
```

#### Event 8: `AddedToConversation`
**Được thêm vào group**

```javascript
connection.on("AddedToConversation", (conversationId) => {
  // Similar to NewConversation
  loadConversation(conversationId);
});
```

#### Event 9: `RemovedFromConversation`
**Bị xóa khỏi group**

```javascript
connection.on("RemovedFromConversation", (conversationId) => {
  // Implementation:
  // Remove conversation khỏi list
  removeConversationFromList(conversationId);
  // Nếu đang ở chat screen đó → Navigate back
  if (currentConversationId === conversationId) {
    navigateBack();
    showToast("You have been removed from this group");
  }
});
```

### Client → Server Methods (Invoke)

Client gọi các methods này trên Hub:

#### Method 1: `JoinConversation`
**Join conversation room để nhận tin real-time**

```javascript
await connection.invoke("JoinConversation", conversationId);
```

**Parameters:**
- `conversationId` (int): ID của conversation

**When to call:**
- Khi vào chat screen
- Khi app quay về foreground và đang ở chat screen

**Mobile Implementation:**
```dart
// Flutter example
class ChatScreen extends StatefulWidget {
  @override
  void initState() {
    super.initState();
    _joinConversation();
  }
  
  Future<void> _joinConversation() async {
    await hubConnection.invoke('JoinConversation', args: [widget.conversationId]);
  }
}
```

#### Method 2: `LeaveConversation`
**Rời conversation room**

```javascript
await connection.invoke("LeaveConversation", conversationId);
```

**When to call:**
- Khi thoát chat screen
- Khi app đi vào background

**Mobile Implementation:**
```dart
@override
void dispose() {
  _leaveConversation();
  super.dispose();
}

Future<void> _leaveConversation() async {
  await hubConnection.invoke('LeaveConversation', args: [widget.conversationId]);
}
```

#### Method 3: `SendTypingIndicator`
**Gửi trạng thái đang gõ**

```javascript
await connection.invoke("SendTypingIndicator", conversationId, isTyping);
```

**Parameters:**
- `conversationId` (int): ID conversation
- `isTyping` (bool): true = đang gõ, false = ngừng gõ

**Mobile Implementation:**
```dart
TextField(
  onChanged: (text) {
    if (text.isNotEmpty && !_isTyping) {
      _isTyping = true;
      hubConnection.invoke('SendTypingIndicator', args: [conversationId, true]);
      
      // Auto stop typing after 3 seconds
      _typingTimer?.cancel();
      _typingTimer = Timer(Duration(seconds: 3), () {
        _isTyping = false;
        hubConnection.invoke('SendTypingIndicator', args: [conversationId, false]);
      });
    } else if (text.isEmpty && _isTyping) {
      _isTyping = false;
      hubConnection.invoke('SendTypingIndicator', args: [conversationId, false]);
    }
  },
)
```

**Best Practice:**
- Debounce typing indicator (không gửi mỗi keystroke)
- Auto-stop after 3 seconds không gõ
- Stop khi gửi tin

#### Method 4: `GetOnlineStatus`
**Check online status của users**

```javascript
const statuses = await connection.invoke("GetOnlineStatus", [userId1, userId2, userId3]);
// Returns: Array<OnlineStatusResponse>
// [
//   { userId: 1, isOnline: true, lastSeen: null },
//   { userId: 2, isOnline: false, lastSeen: "2026-02-07T17:00:00Z" }
// ]
```

**Parameters:**
- `userIds` (array[int]): Danh sách user IDs muốn check

**When to call:**
- Khi load conversation details lần đầu
- Khi refresh conversation list

### Connection Lifecycle

```javascript
// 1. Setup connection
const connection = createConnection();

// 2. Setup event handlers
connection.on("ReceiveMessage", handleMessage);
connection.on("UserTyping", handleTyping);
// ... other events

// 3. Handle connection events
connection.onclose((error) => {
  console.log("Connection closed", error);
  // Show offline indicator
  showOfflineIndicator();
});

connection.onreconnecting((error) => {
  console.log("Reconnecting...", error);
  // Show reconnecting toast
  showReconnectingToast();
});

connection.onreconnected((connectionId) => {
  console.log("Reconnected", connectionId);
  // Re-join conversations
  rejoinActiveConversations();
  hideOfflineIndicator();
});

// 4. Start connection
await connection.start();

// 5. When app goes to background
onAppBackground(() => {
  // Leave all conversation rooms (optional)
  leaveAllConversations();
  // Don't stop connection - keep receiving notifications
});

// 6. When app goes to foreground
onAppForeground(() => {
  // Re-join active conversation
  if (currentConversationId) {
    connection.invoke("JoinConversation", currentConversationId);
  }
});

// 7. On logout
onLogout(() => {
  connection.stop();
});
```

### Error Handling

```javascript
try {
  await connection.invoke("JoinConversation", conversationId);
} catch (error) {
  console.error("Failed to join conversation", error);
  // Show error toast
  // Retry logic (optional)
}
```

**Common Errors:**
- `HubException`: User không phải member của conversation
- `TimeoutException`: Request timeout (network issue)
- `InvocationException`: Lỗi khi invoke method

---

## 📱 Mobile Integration

### Setup Flow

#### 1. App Start
```
App Launch
  ↓
Login/Get Token
  ↓
Initialize SignalR Connection
  ↓
Setup Event Handlers
  ↓
Start Connection
  ↓
Load Conversation List (REST API)
```

#### 2. Enter Chat Screen
```
Navigate to Chat
  ↓
Join Conversation Room (SignalR)
  ↓
Load Message History (REST API - Page 1)
  ↓
Mark Last Message as Read (REST API)
  ↓
Listen for New Messages (SignalR)
```

#### 3. Send Message
```
User types → Show typing indicator (SignalR)
  ↓
User clicks Send
  ↓
Send via REST API
  ↓
Server broadcasts via SignalR
  ↓
All clients receive via "ReceiveMessage" event
```

#### 4. Exit Chat Screen
```
Leave Chat Screen
  ↓
Stop typing indicator (SignalR)
  ↓
Leave Conversation Room (SignalR)
  ↓
Save scroll position (optional)
```

### Screen Implementations

#### Screen 1: Conversation List Screen

**API Calls:**
1. `GET /api/messaging/conversations` - Load tất cả conversations
2. SignalR: Listen `ReceiveMessage` để update lastMessage
3. SignalR: Listen `UserOnline/Offline` để update status

**UI Elements:**
- Pull to refresh
- Each conversation item:
  - Avatar (single user hoặc group icon)
  - Name (user's fullName hoặc group name)
  - Last message preview (truncate 50 chars)
  - Timestamp (format: "5m ago", "Yesterday", "Jan 15")
  - Unread badge (chỉ hiển thị nếu > 0)
  - Online indicator (green dot nếu online)
  
**Actions:**
- Tap item → Navigate to Chat Screen
- Swipe left → Delete conversation (optional)
- Long press → Pin conversation (optional)

**Real-time Updates:**
- Khi nhận `ReceiveMessage`:
  - Move conversation lên top
  - Update lastMessage
  - Tăng unreadCount (nếu không ở chat đó)
  - Show notification nếu app ở background

#### Screen 2: Chat Screen

**API Calls:**
1. `GET /api/messaging/conversations/{id}` - Load conversation info
2. `GET /api/messaging/conversations/{id}/messages` - Load messages (page 1)
3. `POST /api/messaging/messages/read` - Mark as read
4. SignalR: `JoinConversation` - Join room
5. SignalR: Listen `ReceiveMessage` - Nhận tin mới
6. SignalR: Listen `UserTyping` - Hiển thị typing

**UI Elements:**
- App bar:
  - Back button
  - Avatar + Name
  - Online status
  - Menu (Search, View Members, Settings)
- Messages list (reverse scroll):
  - My messages: Right-aligned, blue bubble
  - Others' messages: Left-aligned, gray bubble
  - Timestamp (group by day)
  - Avatar (for group chat)
  - Sender name (for group chat, above bubble)
  - Read receipts (for sent messages)
- Typing indicator: "User is typing..." (bottom of list)
- Input area:
  - TextField
  - Attachment button
  - Send button
  
**Message Types Rendering:**
- TEXT: Bubble với text
- IMAGE: Image trong bubble, click để fullscreen
- DATE_PLAN: Card với title, date, location, "View" button
- LOCATION: Map snapshot, address, "Open in Maps" button
- VOICE: Audio player với play/pause button
- FILE: File icon, name, size, download button

**Actions:**
- Scroll to top → Load more messages (pagination)
- Type text → Send typing indicator
- Click Send → POST message, clear field
- Long press my message → Delete
- Click rich message → View details
- Pull down → Dismiss keyboard

**Real-time Behavior:**
- Auto scroll down khi nhận tin mới (nếu đang ở bottom)
- Auto mark as read khi tin mới hiện trên màn hình
- Show typing indicator khi nhận `UserTyping` event
- Update read receipts khi nhận `MessageRead` event

#### Screen 3: New Conversation Screen

**API Calls:**
1. `GET /api/users` (hoặc endpoint search users) - Load danh sách users
2. `POST /api/messaging/conversations/direct/{userId}` - Tạo chat 1-1
3. `POST /api/messaging/conversations` - Tạo group chat

**UI Flow:**

**For Direct Chat:**
1. User browser hoặc search users
2. Click vào user
3. Call `POST /api/messaging/conversations/direct/{userId}`
4. Navigate to chat screen với conversationId nhận được

**For Group Chat:**
1. Click "New Group"
2. Select multiple users
3. Enter group name
4. Call `POST /api/messaging/conversations` với type=GROUP
5. Navigate to chat screen

#### Screen 4: Group Settings Screen

**API Calls:**
1. `GET /api/messaging/conversations/{id}` - Load group info
2. `POST /api/messaging/conversations/{id}/members` - Add members
3. `DELETE /api/messaging/conversations/{id}/members/{userId}` - Remove member
4. `POST /api/messaging/conversations/{id}/leave` - Leave group

**UI Elements:**
- Group name
- Members list:
  - Avatar
  - Name
  - Role badge (ADMIN)
  - Online status
  - Remove button (nếu là ADMIN)
- Add members button (chỉ ADMIN)
- Leave group button

### Background Behavior

**When app goes to background:**
```
1. Keep SignalR connection alive
2. Leave conversation rooms (optional, để tiết kiệm resources)
3. Continue listening ReceiveMessage
4. When receive message → Show push notification
```

**When app comes to foreground:**
```
1. Re-join active conversation (nếu đang ở chat screen)
2. Refresh conversation list
3. Check online status
```

**Push Notifications Integration:**
```
SignalR ReceiveMessage
  ↓
Is app in background?
  ↓ Yes
Show local notification
  {
    title: "Sender Name",
    body: "Message preview",
    data: { conversationId, messageId }
  }
  ↓
User taps notification
  ↓
Open app → Navigate to Chat Screen
```

---

## 📊 Data Models

### ConversationResponse
```typescript
{
  id: number;
  type: "DIRECT" | "GROUP";
  name: string | null;          // null for DIRECT
  createdBy: number;
  createdAt: string;             // ISO 8601
  members: ConversationMemberResponse[];
  lastMessage: MessageResponse | null;
  unreadCount: number;
}
```

### ConversationMemberResponse
```typescript
{
  userId: number;
  username: string;              // Email
  fullName: string;
  avatar: string | null;
  role: "MEMBER" | "ADMIN";
  joinedAt: string;
  isOnline: boolean;
}
```

### MessageResponse
```typescript
{
  id: number;
  conversationId: number;
  senderId: number;
  senderName: string;
  senderAvatar: string | null;
  content: string;
  messageType: "TEXT" | "IMAGE" | "FILE" | "DATE_PLAN" | "LOCATION" | "EVENT" | "POLL" | "VOICE";
  referenceId: number | null;
  referenceType: string | null;
  metadata: string | null;       // JSON string
  createdAt: string;
  updatedAt: string | null;
  isMine: boolean;
}
```

### TypingIndicatorResponse
```typescript
{
  conversationId: number;
  userId: number;
  username: string;
  isTyping: boolean;
}
```

### OnlineStatusResponse
```typescript
{
  userId: number;
  isOnline: boolean;
  lastSeen: string | null;       // ISO 8601
}
```

---

## 🎬 Flow Examples

### Flow 1: User A sends message to User B

```
Step 1: User A types "Hello"
  ├─ Client A: TextField onChange
  ├─ Client A → SignalR: SendTypingIndicator(conversationId, true)
  ├─ Server → Client B: "UserTyping" event
  └─ Client B: Show "User A is typing..."

Step 2: User A clicks Send
  ├─ Client A → REST API: POST /api/messaging/messages
  ├─ Server: Save to database
  ├─ Server → Client A: Return MessageResponse (201)
  ├─ Client A: Add message to chat (status: sent)
  ├─ Server → SignalR → Client B: "ReceiveMessage" event
  └─ Client B: Add message to chat, play sound

Step 3: User B views message
  ├─ Client B: Message visible on screen
  ├─ Client B → REST API: POST /api/messaging/messages/read
  ├─ Server: Update lastReadMessageId
  ├─ Server → SignalR → Client A: "MessageRead" event
  └─ Client A: Show read receipt (✓✓ or "Seen")
```

### Flow 2: Create Group Chat

```
Step 1: User A creates group
  ├─ Client A: Select users [B, C, D]
  ├─ Client A: Enter group name "Team Chat"
  ├─ Client A → REST API: POST /api/messaging/conversations
  │   Body: { type: "GROUP", name: "Team Chat", memberIds: [2,3,4] }
  └─ Server: Create conversation + members, return ConversationResponse

Step 2: Notify members
  ├─ Server → SignalR → Client B: "NewConversation" event (conversationId: 1)
  ├─ Server → SignalR → Client C: "NewConversation" event
  ├─ Server → SignalR → Client D: "NewConversation" event
  ├─ Client B: Show notification "You were added to Team Chat"
  ├─ Client B → REST API: GET /api/messaging/conversations/1
  └─ Client B: Add conversation to list

Step 3: Navigate to chat
  ├─ Client A: Navigate to chat screen
  ├─ Client A → SignalR: JoinConversation(1)
  ├─ Client A → REST API: GET /api/messaging/conversations/1/messages
  └─ Client A: Display empty chat, ready to send first message
```

### Flow 3: Real-time Online Status

```
Step 1: User A opens app
  ├─ Client A: Login → Get token
  ├─ Client A → SignalR: Connect with token
  ├─ Server: OnConnectedAsync triggered
  ├─ Server: Add userId → connectionId mapping
  └─ Server → SignalR → Others: "UserOnline" event (userId: A)

Step 2: User B receives online event
  ├─ Client B: Receive "UserOnline" event
  ├─ Client B: Update User A's status to online
  └─ Client B: Show green dot on User A's avatar

Step 3: User A closes app
  ├─ SignalR: Connection closed
  ├─ Server: OnDisconnectedAsync triggered
  ├─ Server: Remove connectionId, check if user has other connections
  ├─ No more connections → User is offline
  └─ Server → SignalR → Others: "UserOffline" event (userId: A, lastSeen: now)

Step 4: User B receives offline event
  ├─ Client B: Receive "UserOffline" event
  ├─ Client B: Update User A's status to offline
  └─ Client B: Show "Last seen 2 minutes ago"
```

---

## ⚠️ Error Handling

### HTTP Errors

| Status Code | Meaning | Handling |
|-------------|---------|----------|
| 400 Bad Request | Validation error, missing fields | Show error message to user, highlight invalid fields |
| 401 Unauthorized | Token expired or invalid | Redirect to login, refresh token |
| 403 Forbidden | No permission (e.g., not conversation member) | Show "Access denied" message, navigate back |
| 404 Not Found | Resource not found | Show "Not found" message |
| 500 Server Error | Server issue | Show "Server error, please try again", retry button |

### SignalR Errors

| Error | Cause | Handling |
|-------|-------|----------|
| HubException | Invoke failed (e.g., not member) | Show error toast, don't retry |
| Connection failed | Network issue | Show "Connecting..." spinner, auto-retry |
| Timeout | Request took too long | Show "Connection timeout", retry button |

### Network Issues

**No internet connection:**
```
1. Detect network state change
2. Show offline banner at top
3. Disable send button
4. Queue messages locally (optional)
5. When online:
   - Hide banner
   - Reconnect SignalR
   - Send queued messages
   - Reload conversations
```

**Poor connection:**
```
1. Show "Poor connection" warning
2. Reduce image quality
3. Disable auto-download media
4. Increase timeout values
```

---

## ✅ Best Practices

### 1. Token Management
- Store token securely (Keychain/Keystore)
- Check token expiry before requests
- Implement auto token refresh
- Clear token on logout

### 2. SignalR Connection
- Use `withAutomaticReconnect()` với exponential backoff
- Don't create multiple connections
- Reuse single connection across app
- Properly dispose connection on logout

### 3. Message Caching
- Cache messages locally (SQLite, Hive, Realm)
- Load from cache first, then fetch from API
- Sync cache with server on reconnect
- Clear cache on logout

### 4. Performance
- Implement pagination for messages (don't load all)
- Lazy load images/media
- Use virtual scrolling for long message lists
- Debounce typing indicators
- Throttle scroll events

### 5. UX Improvements
- Optimistic UI: Show sent message immediately
- Retry failed messages
- Show connection status indicator
- Group messages by date
- Show timestamp on long press
- Auto-scroll to bottom on new message (if already at bottom)
- Preserve scroll position on pagination

### 6. Notifications
- Request notification permission on first launch
- Different notification channels (messages, mentions, group updates)
- Customize notification sound
- Badge count for unread messages
- Clear notification when open chat

### 7. Security
- Validate all inputs
- Sanitize message content (prevent XSS if showing HTML)
- Don't store sensitive data in metadata
- Use HTTPS for all API calls
- Use WSS for SignalR

### 8. Testing
- Test with poor network
- Test with multiple devices
- Test offline mode
- Test background behavior
- Test push notifications

---

## 🔍 FAQ

### Q1: Có cần implement chat ở client không, hay server tự broadcast?
**A:** 
- Gửi tin: Call REST API
- Server tự động broadcast qua SignalR
- Client chỉ cần listen event `ReceiveMessage`

### Q2: Làm sao biết tin đã gửi thành công?
**A:**
- Check response của POST message API (201 = success)
- Hoặc đợi nhận lại tin qua SignalR event
- Implement message status: sending → sent → delivered → read

### Q3: Typing indicator có lưu vào DB không?
**A:** 
- Không, chỉ real-time qua SignalR
- Auto-clear sau 3 giây
- Không persist

### Q4: Làm sao load tin nhắn cũ?
**A:**
- Dùng pagination: GET messages với pageNumber tăng dần
- Khi user scroll lên top → Load page tiếp
- Cache locally để tránh load lại

### Q5: App background vẫn nhận tin không?
**A:**
- iOS/Android: Cần implement push notifications
- SignalR connection có thể bị đứt khi app background lâu
- Best practice: Dùng Firebase FCM cho push notification
- Khi app foreground lại → Reconnect SignalR → Sync messages

### Q6: Xử lý tin gửi thất bại như thế nào?
**A:**
- Save tin ở trạng thái "failed" với error
- Show retry button
- Implement retry queue
- User có thể delete tin failed

### Q7: Group chat với 1000 members có được không?
**A:**
- Được, nhưng nên có limit
- Performance issue khi broadcast đến quá nhiều connections
- Recommend: Max 500 members per group
- Với > 500: Cân nhắc dùng broadcast channels thay vì personal connections

### Q8: Có thể gửi file/hình không?
**A:**
- Cần implement file upload API riêng
- Upload file trước → Nhận URL
- Send message với messageType = IMAGE/FILE + referenceId = fileId
- Metadata chứa URL, size, filename

---

## 📞 Support

Nếu gặp vấn đề khi integrate:
1. Check token có hợp lệ không
2. Check network connection
3. Check SignalR connection state
4. Check server logs
5. Check API response errors

**Common Issues:**
- "401 Unauthorized" → Token expired, cần login lại
- "403 Forbidden" → Không phải member của conversation
- SignalR not receiving → Chưa join conversation room
- Messages not showing → Chưa setup event handler
- Typing indicator not working → Chưa gọi SendTypingIndicator

---

**Document Version:** 1.0  
**Last Updated:** February 7, 2026  
**API Version:** v1
