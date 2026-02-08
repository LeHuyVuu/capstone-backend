# 🚀 Messaging API - Complete Guide for Frontend

> **Đọc KỸ tài liệu này trước khi tích hợp messaging!**

---

## ⚠️ QUAN TRỌNG NHẤT - ĐỌC ĐẦU TIÊN!

### 🔴 Quy Tắc Vàng

**TẤT CẢ API MESSAGING SỬ DỤNG `user_account.id` (User ID)**  
**KHÔNG BAO GIỜ SỬ DỤNG `member_profile.id` (Member Profile ID)**

```
✅ ĐÚNG: Sử dụng user_account.id
❌ SAI: Sử dụng member_profile.id
```

### 📊 Cấu Trúc Database

#### Bảng `user_account` (Authentication & Messaging)
```sql
id    | email              | display_name  | avatar_url
------|--------------------|--------------|-----------
1     | john@gmail.com     | John Doe     | http://...
24    | alice@gmail.com    | Alice        | http://...
```

#### Bảng `member_profile` (User Profile Data)
```sql
id   | user_id | full_name  | bio
-----|---------|------------|-------------
101  | 1       | John Doe   | Hello...
202  | 24      | Alice      | Hi there...
```

### ⚠️ Lưu Ý Cực Kỳ Quan Trọng:

1. **`user_account.id`** → Dùng cho authentication, messaging, chat
2. **`member_profile.id`** → CHỈ dùng cho profile data, KHÔNG dùng cho messaging
3. **TUYỆT ĐỐI KHÔNG được lẫn lộn 2 ID này!**

### 🔥 Ví Dụ Đúng & Sai

```javascript
// Giả sử bạn có member profile response
const memberProfile = {
  id: 202,           // ← member_profile.id (KHÔNG DÙNG!)
  userId: 24,        // ← user_account.id (DÙNG CÁI NÀY!)
  fullName: "Alice",
  bio: "Hi there..."
};

// ✅ ĐÚNG - Chat với Alice
await createDirectConversation(memberProfile.userId);  // 24

// ❌ SAI - LỖI!
await createDirectConversation(memberProfile.id);  // 202
// → Error: User with ID 202 not found
```

---

## 📋 API Endpoints

### Base URL
```
Production: https://couplemood.ooguy.com/api/messaging
SignalR Hub: https://couplemood.ooguy.com/hubs/messaging
```

### Authentication Required
Tất cả API đều cần JWT token:
```http
Authorization: Bearer {your_jwt_token}
```

---

## 1️⃣ Tạo/Lấy Chat 1-1

### Endpoint
```http
POST /api/messaging/conversations/direct/{userId}
Authorization: Bearer {token}
```

### Parameters
- `userId` (path) - **user_account.id** của người muốn chat ✅

### Request Example
```bash
POST /api/messaging/conversations/direct/24
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

### Response (200 OK)
```json
{
  "id": 1,
  "type": "DIRECT",
  "name": null,
  "createdBy": 1,
  "createdAt": "2026-02-07T10:00:00Z",
  "members": [
    {
      "userId": 1,              // ← user_account.id
      "username": "john@gmail.com",
      "fullName": "John Doe",
      "avatar": "https://example.com/avatar/john.jpg",
      "role": "ADMIN",
      "joinedAt": "2026-02-07T10:00:00Z",
      "isOnline": true
    },
    {
      "userId": 24,             // ← user_account.id
      "username": "alice@gmail.com",
      "fullName": "Alice",
      "avatar": "https://example.com/avatar/alice.jpg",
      "role": "MEMBER",
      "joinedAt": "2026-02-07T10:00:00Z",
      "isOnline": false
    }
  ],
  "lastMessage": null,
  "unreadCount": 0
}
```

### Error Response (400 Bad Request)
```json
{
  "message": "User with ID 24 not found",
  "code": 400,
  "data": null,
  "traceId": "0HNJ6TN16GJ5P:00000001"
}
```

---

## 2️⃣ Tạo Group Chat

### Endpoint
```http
POST /api/messaging/conversations
Content-Type: application/json
Authorization: Bearer {token}
```

### Request Body
```json
{
  "type": "GROUP",
  "name": "Team Chat",
  "memberIds": [24, 35, 42]  // ← Array of user_account.id
}
```

### Response (201 Created)
```json
{
  "id": 5,
  "type": "GROUP",
  "name": "Team Chat",
  "createdBy": 1,
  "createdAt": "2026-02-08T10:00:00Z",
  "members": [
    {
      "userId": 1,
      "username": "john@gmail.com",
      "fullName": "John Doe",
      "avatar": "https://example.com/avatar/john.jpg",
      "role": "ADMIN",
      "joinedAt": "2026-02-08T10:00:00Z",
      "isOnline": true
    },
    {
      "userId": 24,
      "username": "alice@gmail.com",
      "fullName": "Alice",
      "avatar": "https://example.com/avatar/alice.jpg",
      "role": "MEMBER",
      "joinedAt": "2026-02-08T10:00:00Z",
      "isOnline": false
    }
  ],
  "lastMessage": null,
  "unreadCount": 0
}
```

---

## 3️⃣ Lấy Danh Sách Conversations

### Endpoint
```http
GET /api/messaging/conversations
Authorization: Bearer {token}
```

### Response (200 OK)
```json
[
  {
    "id": 1,
    "type": "DIRECT",
    "name": null,
    "createdBy": 1,
    "createdAt": "2026-02-07T10:00:00Z",
    "members": [
      {
        "userId": 1,
        "username": "john@gmail.com",
        "fullName": "John Doe",
        "avatar": "https://example.com/avatar/john.jpg",
        "role": "ADMIN",
        "joinedAt": "2026-02-07T10:00:00Z",
        "isOnline": true
      },
      {
        "userId": 24,
        "username": "alice@gmail.com",
        "fullName": "Alice",
        "avatar": "https://example.com/avatar/alice.jpg",
        "role": "MEMBER",
        "joinedAt": "2026-02-07T10:00:00Z",
        "isOnline": false
      }
    ],
    "lastMessage": {
      "id": 123,
      "conversationId": 1,
      "senderId": 24,           // ← user_account.id
      "senderName": "Alice",
      "senderAvatar": "https://example.com/avatar/alice.jpg",
      "content": "Hi! How are you?",
      "messageType": "TEXT",
      "createdAt": "2026-02-08T09:30:00Z",
      "isMine": false
    },
    "unreadCount": 2
  }
]
```

---

## 4️⃣ Lấy Messages

### Endpoint
```http
GET /api/messaging/conversations/{conversationId}/messages?pageNumber=1&pageSize=50
Authorization: Bearer {token}
```

### Query Parameters
- `pageNumber` (optional, default: 1) - Trang hiện tại
- `pageSize` (optional, default: 50) - Số messages mỗi trang

### Response (200 OK)
```json
{
  "messages": [
    {
      "id": 125,
      "conversationId": 1,
      "senderId": 1,            // ← user_account.id
      "senderName": "John Doe",
      "senderAvatar": "https://example.com/avatar/john.jpg",
      "content": "I'm good, thanks!",
      "messageType": "TEXT",
      "referenceId": null,
      "referenceType": null,
      "metadata": null,
      "createdAt": "2026-02-08T09:35:00Z",
      "updatedAt": null,
      "isMine": true
    },
    {
      "id": 124,
      "conversationId": 1,
      "senderId": 24,           // ← user_account.id
      "senderName": "Alice",
      "senderAvatar": "https://example.com/avatar/alice.jpg",
      "content": "How are you?",
      "messageType": "TEXT",
      "referenceId": null,
      "referenceType": null,
      "metadata": null,
      "createdAt": "2026-02-08T09:31:00Z",
      "updatedAt": null,
      "isMine": false
    }
  ],
  "pageNumber": 1,
  "pageSize": 50,
  "totalPages": 1,
  "hasNextPage": false
}
```

---

## 5️⃣ Gửi Message

### Endpoint
```http
POST /api/messaging/messages
Content-Type: application/json
Authorization: Bearer {token}
```

### Request Body - Text Message
```json
{
  "conversationId": 1,
  "content": "Hello everyone!",
  "messageType": "TEXT"
}
```

### Request Body - Date Plan Message
```json
{
  "conversationId": 1,
  "content": "Check out this date plan!",
  "messageType": "DATE_PLAN",
  "referenceId": 42,
  "referenceType": "DatePlan"
}
```

### Request Body - Location Message
```json
{
  "conversationId": 1,
  "content": "Let's meet here!",
  "messageType": "LOCATION",
  "referenceId": 100,
  "referenceType": "VenueLocation"
}
```

### Response (200 OK)
```json
{
  "id": 126,
  "conversationId": 1,
  "senderId": 1,
  "senderName": "John Doe",
  "senderAvatar": "https://example.com/avatar/john.jpg",
  "content": "Hello everyone!",
  "messageType": "TEXT",
  "referenceId": null,
  "referenceType": null,
  "metadata": null,
  "createdAt": "2026-02-08T10:00:00Z",
  "updatedAt": null,
  "isMine": true
}
```

---

## 6️⃣ Mark Message as Read

### Endpoint
```http
POST /api/messaging/messages/read
Content-Type: application/json
Authorization: Bearer {token}
```

### Request Body
```json
{
  "conversationId": 1,
  "messageId": 123
}
```

### Response (200 OK)
```json
{
  "message": "Success",
  "code": 200
}
```

---

## 7️⃣ Thêm Members Vào Group

### Endpoint
```http
POST /api/messaging/conversations/members
Content-Type: application/json
Authorization: Bearer {token}
```

### Request Body
```json
{
  "conversationId": 5,
  "memberIds": [50, 51]  // ← Array of user_account.id
}
```

### Response (200 OK)
```json
{
  "message": "Success",
  "code": 200
}
```

---

## 8️⃣ Xóa Member Khỏi Group

### Endpoint
```http
DELETE /api/messaging/conversations/members
Content-Type: application/json
Authorization: Bearer {token}
```

### Request Body
```json
{
  "conversationId": 5,
  "userId": 50  // ← user_account.id
}
```

### Response (200 OK)
```json
{
  "message": "Success",
  "code": 200
}
```

---

## 9️⃣ Delete Message

### Endpoint
```http
DELETE /api/messaging/messages/{messageId}
Authorization: Bearer {token}
```

### Response (200 OK)
```json
{
  "message": "Success",
  "code": 200
}
```

---

## 🔟 Search Messages

### Endpoint
```http
GET /api/messaging/conversations/{conversationId}/messages/search?searchTerm=hello
Authorization: Bearer {token}
```

### Query Parameters
- `searchTerm` (required) - Từ khóa tìm kiếm

### Response (200 OK)
```json
[
  {
    "id": 123,
    "conversationId": 1,
    "senderId": 24,
    "senderName": "Alice",
    "content": "Hello! How are you?",
    "messageType": "TEXT",
    "createdAt": "2026-02-08T09:30:00Z",
    "isMine": false
  }
]
```

---

## 💻 Code Examples - React/React Native

### Setup Axios Instance

```javascript
import axios from 'axios';

const api = axios.create({
  baseURL: 'https://couplemood.ooguy.com/api/messaging',
  headers: {
    'Content-Type': 'application/json'
  }
});

// Add token to every request
api.interceptors.request.use(config => {
  const token = localStorage.getItem('accessToken');
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

// Handle errors
api.interceptors.response.use(
  response => response,
  error => {
    if (error.response?.status === 401) {
      // Token expired, redirect to login
      window.location.href = '/login';
    }
    return Promise.reject(error);
  }
);

export default api;
```

### 1. Tạo Chat 1-1

```javascript
const createDirectChat = async (otherUserId) => {
  try {
    // Validate input
    if (!otherUserId || otherUserId <= 0) {
      throw new Error('Invalid user ID');
    }
    
    // Check không chat với chính mình
    const currentUserId = getCurrentUserId();
    if (currentUserId === otherUserId) {
      throw new Error('Cannot create conversation with yourself');
    }
    
    // Call API
    const { data } = await api.post(`/conversations/direct/${otherUserId}`);
    return data;
  } catch (error) {
    console.error('Error creating conversation:', error);
    throw error;
  }
};

// Sử dụng:
// User click vào profile của Alice
const memberProfile = {
  id: 202,        // member_profile.id
  userId: 24,     // user_account.id ← DÙNG CÁI NÀY!
  fullName: "Alice"
};

const conversation = await createDirectChat(memberProfile.userId);
```

### 2. Load Conversations

```javascript
const getConversations = async () => {
  try {
    const { data } = await api.get('/conversations');
    return data;
  } catch (error) {
    console.error('Error getting conversations:', error);
    throw error;
  }
};

// Sử dụng:
const conversations = await getConversations();
```

### 3. Load Messages

```javascript
const getMessages = async (conversationId, page = 1, pageSize = 50) => {
  try {
    const { data } = await api.get(`/conversations/${conversationId}/messages`, {
      params: {
        pageNumber: page,
        pageSize: pageSize
      }
    });
    return data;
  } catch (error) {
    console.error('Error getting messages:', error);
    throw error;
  }
};

// Sử dụng:
const { messages, hasNextPage } = await getMessages(1, 1, 50);
```

### 4. Gửi Message

```javascript
const sendMessage = async (conversationId, content, messageType = 'TEXT') => {
  try {
    const { data } = await api.post('/messages', {
      conversationId,
      content,
      messageType
    });
    return data;
  } catch (error) {
    console.error('Error sending message:', error);
    throw error;
  }
};

// Sử dụng:
const message = await sendMessage(1, 'Hello!', 'TEXT');
```

### 5. Gửi Date Plan Message

```javascript
const sendDatePlanMessage = async (conversationId, datePlanId, content) => {
  try {
    const { data } = await api.post('/messages', {
      conversationId,
      content,
      messageType: 'DATE_PLAN',
      referenceId: datePlanId,
      referenceType: 'DatePlan'
    });
    return data;
  } catch (error) {
    console.error('Error sending date plan:', error);
    throw error;
  }
};

// Sử dụng:
const message = await sendDatePlanMessage(1, 42, 'Check out this plan!');
```

### 6. Tạo Group Chat

```javascript
const createGroupChat = async (name, memberIds) => {
  try {
    // memberIds phải là array of user_account.id
    const { data } = await api.post('/conversations', {
      type: 'GROUP',
      name,
      memberIds
    });
    return data;
  } catch (error) {
    console.error('Error creating group:', error);
    throw error;
  }
};

// Sử dụng:
const group = await createGroupChat('Team Chat', [24, 35, 42]);
```

---

## 📱 Code Examples - Flutter/Dart

### Setup Dio Instance

```dart
import 'package:dio/dio.dart';
import 'package:shared_preferences/shared_preferences.dart';

class MessagingApi {
  late Dio _dio;
  static const String baseUrl = 'https://couplemood.ooguy.com/api/messaging';

  MessagingApi() {
    _dio = Dio(BaseOptions(
      baseUrl: baseUrl,
      connectTimeout: const Duration(seconds: 10),
      receiveTimeout: const Duration(seconds: 10),
    ));

    // Add token interceptor
    _dio.interceptors.add(InterceptorsWrapper(
      onRequest: (options, handler) async {
        final prefs = await SharedPreferences.getInstance();
        final token = prefs.getString('accessToken');
        if (token != null) {
          options.headers['Authorization'] = 'Bearer $token';
        }
        return handler.next(options);
      },
      onError: (error, handler) {
        if (error.response?.statusCode == 401) {
          // Token expired, redirect to login
        }
        return handler.next(error);
      },
    ));
  }

  // 1. Tạo chat 1-1
  Future<Map<String, dynamic>> createDirectChat(int otherUserId) async {
    try {
      if (otherUserId <= 0) {
        throw Exception('Invalid user ID');
      }
      
      final response = await _dio.post('/conversations/direct/$otherUserId');
      return response.data as Map<String, dynamic>;
    } catch (e) {
      print('Error creating conversation: $e');
      rethrow;
    }
  }

  // 2. Load conversations
  Future<List<dynamic>> getConversations() async {
    try {
      final response = await _dio.get('/conversations');
      return response.data as List<dynamic>;
    } catch (e) {
      print('Error getting conversations: $e');
      rethrow;
    }
  }

  // 3. Load messages
  Future<Map<String, dynamic>> getMessages(
    int conversationId, {
    int pageNumber = 1,
    int pageSize = 50,
  }) async {
    try {
      final response = await _dio.get(
        '/conversations/$conversationId/messages',
        queryParameters: {
          'pageNumber': pageNumber,
          'pageSize': pageSize,
        },
      );
      return response.data as Map<String, dynamic>;
    } catch (e) {
      print('Error getting messages: $e');
      rethrow;
    }
  }

  // 4. Gửi message
  Future<Map<String, dynamic>> sendMessage({
    required int conversationId,
    required String content,
    String messageType = 'TEXT',
    int? referenceId,
    String? referenceType,
  }) async {
    try {
      final response = await _dio.post('/messages', data: {
        'conversationId': conversationId,
        'content': content,
        'messageType': messageType,
        if (referenceId != null) 'referenceId': referenceId,
        if (referenceType != null) 'referenceType': referenceType,
      });
      return response.data as Map<String, dynamic>;
    } catch (e) {
      print('Error sending message: $e');
      rethrow;
    }
  }
}
```

### Sử dụng:

```dart
final api = MessagingApi();

// Tạo chat với user
final conversation = await api.createDirectChat(24);

// Load conversations
final conversations = await api.getConversations();

// Load messages
final messagesData = await api.getMessages(1, pageNumber: 1);

// Gửi message
final message = await api.sendMessage(
  conversationId: 1,
  content: 'Hello!',
  messageType: 'TEXT',
);
```

---

## 🎨 UI/UX Implementation

### Hiển Thị Conversation List

```javascript
const renderConversationList = (conversations) => {
  const currentUserId = getCurrentUserId();
  
  return conversations.map(conv => {
    let displayName, avatar;
    
    if (conv.type === 'DIRECT') {
      // Direct chat: Hiển thị thông tin người còn lại
      const otherMember = conv.members.find(m => m.userId !== currentUserId);
      displayName = otherMember.fullName || otherMember.username;
      avatar = otherMember.avatar || 'default_avatar.png';
    } else {
      // Group chat
      displayName = conv.name;
      avatar = 'group_icon.png';
    }
    
    const lastMessagePreview = conv.lastMessage 
      ? `${conv.lastMessage.senderName}: ${conv.lastMessage.content}`
      : 'No messages yet';
    
    return (
      <ConversationItem
        key={conv.id}
        id={conv.id}
        name={displayName}
        avatar={avatar}
        lastMessage={lastMessagePreview}
        unreadCount={conv.unreadCount}
        timestamp={conv.lastMessage?.createdAt}
        onClick={() => openConversation(conv.id)}
      />
    );
  });
};
```

### Hiển Thị Messages

```javascript
const renderMessages = (messages) => {
  return messages.map(msg => {
    const align = msg.isMine ? 'right' : 'left';
    const bubbleColor = msg.isMine ? 'blue' : 'gray';
    
    let messageContent;
    
    switch (msg.messageType) {
      case 'TEXT':
        messageContent = <Text>{msg.content}</Text>;
        break;
        
      case 'DATE_PLAN':
        // Gọi API lấy date plan info
        messageContent = <DatePlanCard datePlanId={msg.referenceId} />;
        break;
        
      case 'LOCATION':
        // Gọi API lấy location info
        messageContent = <LocationCard locationId={msg.referenceId} />;
        break;
        
      default:
        messageContent = <Text>{msg.content}</Text>;
    }
    
    return (
      <MessageBubble
        key={msg.id}
        align={align}
        bubbleColor={bubbleColor}
        senderName={msg.isMine ? 'You' : msg.senderName}
        avatar={msg.senderAvatar}
        content={messageContent}
        timestamp={formatTime(msg.createdAt)}
      />
    );
  });
};
```

---

## 📊 Message Types

| messageType | Description | Có referenceId? | Frontend Action |
|-------------|-------------|-----------------|-----------------|
| `TEXT` | Tin nhắn text thông thường | Không | Hiển thị text |
| `DATE_PLAN` | Share date plan | Có | Gọi API `/api/dateplan/{referenceId}` → Hiển thị card |
| `LOCATION` | Share location/venue | Có | Gọi API `/api/venuelocation/{referenceId}` → Hiển thị map |
| `IMAGE` | Tin nhắn hình ảnh | Có | Hiển thị image |
| `FILE` | Tin nhắn file | Có | Hiển thị download link |

---

## 🎯 Use Cases Thực Tế

### Case 1: User Click Vào Profile Để Chat

```javascript
// API trả về member profile
const memberProfile = {
  id: 202,           // member_profile.id (KHÔNG DÙNG!)
  userId: 24,        // user_account.id (DÙNG CÁI NÀY!)
  fullName: "Alice",
  bio: "..."
};

// ✅ ĐÚNG
const handleChatClick = async () => {
  const conversation = await createDirectChat(memberProfile.userId);
  navigateToChat(conversation.id);
};

// ❌ SAI
const handleChatClick = async () => {
  const conversation = await createDirectChat(memberProfile.id); // LỖI!
  navigateToChat(conversation.id);
};
```

### Case 2: Load Conversation List

```javascript
const ConversationListScreen = () => {
  const [conversations, setConversations] = useState([]);
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    loadConversations();
  }, []);

  const loadConversations = async () => {
    setLoading(true);
    try {
      const data = await getConversations();
      setConversations(data);
    } catch (error) {
      showError(error.message);
    } finally {
      setLoading(false);
    }
  };

  return (
    <FlatList
      data={conversations}
      renderItem={({ item }) => renderConversationItem(item)}
      refreshing={loading}
      onRefresh={loadConversations}
    />
  );
};
```

### Case 3: Chat Screen với Pagination

```javascript
const ChatScreen = ({ conversationId }) => {
  const [messages, setMessages] = useState([]);
  const [page, setPage] = useState(1);
  const [hasMore, setHasMore] = useState(true);
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    loadMessages();
  }, [conversationId]);

  const loadMessages = async (pageNum = 1) => {
    if (loading || !hasMore) return;
    
    setLoading(true);
    try {
      const data = await getMessages(conversationId, pageNum, 50);
      
      if (pageNum === 1) {
        setMessages(data.messages);
      } else {
        setMessages(prev => [...prev, ...data.messages]);
      }
      
      setHasMore(data.hasNextPage);
      setPage(pageNum);
    } catch (error) {
      showError(error.message);
    } finally {
      setLoading(false);
    }
  };

  const handleSendMessage = async (content) => {
    try {
      const message = await sendMessage(conversationId, content);
      setMessages(prev => [message, ...prev]);
    } catch (error) {
      showError(error.message);
    }
  };

  const handleLoadMore = () => {
    if (hasMore && !loading) {
      loadMessages(page + 1);
    }
  };

  return (
    <View>
      <FlatList
        data={messages}
        renderItem={({ item }) => renderMessage(item)}
        onEndReached={handleLoadMore}
        onEndReachedThreshold={0.5}
      />
      <MessageInput onSend={handleSendMessage} />
    </View>
  );
};
```

---

## ❌ Common Errors & Solutions

### Error 1: "User with ID XXX not found"

**Nguyên nhân:** Đang gửi `member_profile.id` thay vì `user_account.id`

**Giải pháp:**
```javascript
// ❌ SAI
const memberId = memberProfile.id; // 202
await createDirectChat(memberId);

// ✅ ĐÚNG
const userId = memberProfile.userId; // 24
await createDirectChat(userId);
```

### Error 2: 401 Unauthorized

**Nguyên nhân:** Token không hợp lệ hoặc hết hạn

**Giải pháp:**
```javascript
// Thêm error handling
api.interceptors.response.use(
  response => response,
  error => {
    if (error.response?.status === 401) {
      // Clear token và redirect về login
      localStorage.removeItem('accessToken');
      window.location.href = '/login';
    }
    return Promise.reject(error);
  }
);
```

### Error 3: "Cannot create conversation with yourself"

**Nguyên nhân:** Đang cố chat với chính mình

**Giải pháp:**
```javascript
const createChat = async (otherUserId) => {
  const currentUserId = getCurrentUserId();
  
  if (currentUserId === otherUserId) {
    alert('Không thể tạo conversation với chính mình!');
    return;
  }
  
  return await createDirectChat(otherUserId);
};
```

### Error 4: 404 Conversation Not Found

**Nguyên nhân:** Conversation ID không tồn tại hoặc user không có quyền truy cập

**Giải pháp:**
```javascript
try {
  const messages = await getMessages(conversationId);
} catch (error) {
  if (error.response?.status === 404) {
    alert('Conversation không tồn tại!');
    navigateBack();
  }
}
```

---

## 💡 Best Practices

### 1. Validate User ID Trước Khi Call API

```javascript
const createDirectChat = async (otherUserId) => {
  // Validate input
  if (!otherUserId || otherUserId <= 0) {
    throw new Error('Invalid user ID');
  }
  
  // Check không chat với chính mình
  const currentUserId = getCurrentUserId();
  if (currentUserId === otherUserId) {
    throw new Error('Cannot chat with yourself');
  }
  
  // Call API
  return await api.post(`/conversations/direct/${otherUserId}`);
};
```

### 2. Handle Loading và Error States

```javascript
const [loading, setLoading] = useState(false);
const [error, setError] = useState(null);

const loadData = async () => {
  setLoading(true);
  setError(null);
  
  try {
    const data = await getConversations();
    setConversations(data);
  } catch (err) {
    setError(err.message);
  } finally {
    setLoading(false);
  }
};
```

### 3. Cache User Info

```javascript
const userCache = new Map();

const getUserInfo = async (userId) => {
  if (userCache.has(userId)) {
    return userCache.get(userId);
  }
  
  const user = await fetchUserById(userId);
  userCache.set(userId, user);
  return user;
};
```

### 4. Optimistic UI Updates

```javascript
const handleSendMessage = async (content) => {
  // Tạo temporary message
  const tempMessage = {
    id: `temp-${Date.now()}`,
    content,
    senderId: currentUserId,
    isMine: true,
    createdAt: new Date().toISOString(),
    sending: true
  };
  
  // Hiển thị ngay
  setMessages(prev => [tempMessage, ...prev]);
  
  try {
    // Gửi API
    const message = await sendMessage(conversationId, content);
    
    // Replace temp message với message thật
    setMessages(prev => 
      prev.map(m => m.id === tempMessage.id ? message : m)
    );
  } catch (error) {
    // Xóa temp message nếu fail
    setMessages(prev => prev.filter(m => m.id !== tempMessage.id));
    showError('Failed to send message');
  }
};
```

---

## 🔄 Data Mapping Reference

| Frontend Data | API Field | Type | Database Source |
|---------------|-----------|------|-----------------|
| Conversation ID | `id` | int | `conversations.id` |
| **User ID** | `userId` | int | **`user_account.id`** ✅ |
| ~~Member Profile ID~~ | - | - | ❌ **KHÔNG DÙNG** |
| Sender ID | `senderId` | int | **`user_account.id`** ✅ |
| Message ID | `id` | int | `messages.id` |
| Conversation Type | `type` | string | `DIRECT` hoặc `GROUP` |

---

## ✅ Testing Checklist

- [ ] Test tạo direct conversation với user ID hợp lệ
- [ ] Test tạo direct conversation với user ID không tồn tại (expect error 400)
- [ ] Test tạo direct conversation với chính mình (expect error)
- [ ] Test tạo group conversation
- [ ] Test load conversations
- [ ] Test load messages với pagination
- [ ] Test gửi text message
- [ ] Test gửi date plan message
- [ ] Test gửi location message
- [ ] Test mark as read
- [ ] Test thêm member vào group
- [ ] Test xóa member khỏi group
- [ ] Test delete message
- [ ] Test search messages
- [ ] Test error handling (401, 404, 400)
- [ ] Test với token hết hạn
- [ ] Test UI hiển thị đúng `isMine` flag
- [ ] Test pagination load more
- [ ] Test optimistic UI updates

---

## 🧪 Test với cURL

### 1. Login để lấy token
```bash
curl -X POST "https://couplemood.ooguy.com/api/auth/login" \
  -H "Content-Type: application/json" \
  -d '{
    "email": "your@email.com",
    "password": "yourpassword"
  }'
```

### 2. Tạo conversation (thay YOUR_TOKEN)
```bash
curl -X POST "https://couplemood.ooguy.com/api/messaging/conversations/direct/24" \
  -H "Authorization: Bearer YOUR_TOKEN"
```

### 3. Gửi message
```bash
curl -X POST "https://couplemood.ooguy.com/api/messaging/messages" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "conversationId": 1,
    "content": "Hello!",
    "messageType": "TEXT"
  }'
```

---

## 📞 Need Help?

### Debugging Steps:

1. **Kiểm tra đang dùng đúng User ID chưa:**
   ```javascript
   console.log('Member Profile ID:', memberProfile.id);      // 202
   console.log('User ID:', memberProfile.userId);            // 24
   console.log('Sending to API:', memberProfile.userId);     // Phải là 24!
   ```

2. **Kiểm tra token:**
   ```javascript
   const token = localStorage.getItem('accessToken');
   console.log('Token:', token);
   console.log('Token exists:', !!token);
   ```

3. **Check error response:**
   ```javascript
   try {
     await createDirectChat(userId);
   } catch (error) {
     console.log('Status:', error.response?.status);
     console.log('Message:', error.response?.data?.message);
     console.log('TraceId:', error.response?.data?.traceId);  // Report này cho backend
   }
   ```

### Common Issues:

| Issue | Check | Solution |
|-------|-------|----------|
| 500 Error | User ID | Đảm bảo dùng `user_account.id`, không phải `member_profile.id` |
| 401 Error | Token | Kiểm tra token còn hạn, refresh hoặc login lại |
| 404 Error | Conversation ID | Kiểm tra conversation tồn tại và user có quyền truy cập |
| 400 Error | Request Body | Kiểm tra format JSON và required fields |

---

## 🎓 Summary - Những Điều Quan Trọng Nhất

### ✅ ALWAYS DO:
1. Sử dụng `user_account.id` cho messaging
2. Gửi JWT token trong header
3. Validate input trước khi call API
4. Handle loading và error states
5. Check `isMine` flag từ response để hiển thị UI
6. Implement pagination cho messages

### ❌ NEVER DO:
1. Sử dụng `member_profile.id` cho messaging API
2. Quên gửi Authorization header
3. Chat với chính mình
4. Bỏ qua error handling
5. Load tất cả messages một lúc (không pagination)

---

**Last Updated:** February 8, 2026  
**API Version:** 1.0.0  
**Base URL:** https://couplemood.ooguy.com/api/messaging
