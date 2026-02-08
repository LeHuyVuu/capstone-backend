import { useState, useEffect } from 'react'
import NewConversationModal from './NewConversationModal'
import { userService } from '../services/userService'
import { chatService } from '../services/chatService'

function Sidebar({ conversations, selectedConversation, onSelectConversation, onLogout, onRefresh, currentUserId }) {
  const [showNewConversationModal, setShowNewConversationModal] = useState(false)
  const [activeTab, setActiveTab] = useState('conversations') // 'conversations' or 'users'
  const [users, setUsers] = useState([])
  const [loadingUsers, setLoadingUsers] = useState(false)
  const [searchTerm, setSearchTerm] = useState('')

  useEffect(() => {
    if (activeTab === 'users') {
      loadUsers()
    }
  }, [activeTab])

  useEffect(() => {
    if (activeTab === 'users' && searchTerm !== '') {
      const timer = setTimeout(() => {
        loadUsers()
      }, 500) // Debounce 500ms
      return () => clearTimeout(timer)
    }
  }, [searchTerm])

  const loadUsers = async () => {
    setLoadingUsers(true)
    try {
      const data = await userService.getAllUsers(100, searchTerm)
      // Filter out current user
      const filteredUsers = data.filter(u => u.id !== currentUserId)
      setUsers(filteredUsers)
    } catch (error) {
      console.error('Error loading users:', error)
    } finally {
      setLoadingUsers(false)
    }
  }

  const handleConversationCreated = (conversation) => {
    onRefresh()
    onSelectConversation(conversation)
  }

  const handleUserClick = async (user) => {
    try {
      // Create or get direct conversation with this user
      const conversation = await chatService.getOrCreateDirectConversation(Number(user.id))
      console.log('Created/got conversation:', conversation)
      
      if (conversation && conversation.id) {
        // Immediately select conversation first (this will load messages and show chat area)
        onSelectConversation(conversation)
        
        // Then refresh conversations list in background
        onRefresh()
        
        // Switch to conversations tab to show it in the list too
        setActiveTab('conversations')
      } else {
        console.error('Invalid conversation response:', conversation)
        alert('Không thể tạo cuộc trò chuyện - phản hồi không hợp lệ')
      }
    } catch (error) {
      console.error('Error creating conversation:', error)
      alert('Không thể tạo cuộc trò chuyện: ' + (error.response?.data?.message || error.message))
    }
  }

  return (
    <div className="sidebar">
      <div className="sidebar-header">
        <h2>Chat</h2>
        <button onClick={onLogout} className="logout-btn">
          Đăng xuất
        </button>
      </div>

      {/* Tabs */}
      <div style={{ 
        display: 'flex', 
        borderBottom: '1px solid #e0e0e0',
        background: '#f8f9fa'
      }}>
        <button
          onClick={() => setActiveTab('conversations')}
          style={{
            flex: 1,
            padding: '15px',
            border: 'none',
            background: activeTab === 'conversations' ? 'white' : 'transparent',
            borderBottom: activeTab === 'conversations' ? '2px solid #667eea' : 'none',
            cursor: 'pointer',
            fontWeight: activeTab === 'conversations' ? 600 : 400,
            color: activeTab === 'conversations' ? '#667eea' : '#666'
          }}
        >
          Tin nhắn
        </button>
        <button
          onClick={() => setActiveTab('users')}
          style={{
            flex: 1,
            padding: '15px',
            border: 'none',
            background: activeTab === 'users' ? 'white' : 'transparent',
            borderBottom: activeTab === 'users' ? '2px solid #667eea' : 'none',
            cursor: 'pointer',
            fontWeight: activeTab === 'users' ? 600 : 400,
            color: activeTab === 'users' ? '#667eea' : '#666'
          }}
        >
          Người dùng
        </button>
      </div>

      {activeTab === 'conversations' && (
        <>
          <div style={{ padding: '10px 20px', borderBottom: '1px solid #e0e0e0' }}>
            <button 
              className="btn" 
              onClick={() => setShowNewConversationModal(true)}
              style={{ width: '100%' }}
            >
              + Tạo nhóm chat
            </button>
          </div>
          
          <div className="conversation-list">
            {conversations.map((conv) => {
              // For direct conversations, show the other user's name
              let displayName = conv.name || 'Cuộc trò chuyện'
              if (conv.type === 'DIRECT' && conv.members) {
                const otherMember = conv.members.find(m => m.userId !== currentUserId)
                if (otherMember) {
                  displayName = otherMember.fullName || otherMember.username || displayName
                }
              }
              
              return (
                <div
                  key={conv.id}
                  className={`conversation-item ${selectedConversation?.id === conv.id ? 'active' : ''}`}
                  onClick={() => onSelectConversation(conv)}
                >
                  <div className="conversation-name">
                    <span>{displayName}</span>
                    {conv.unreadCount > 0 && (
                      <span className="unread-badge">{conv.unreadCount}</span>
                    )}
                  </div>
                  <div className="conversation-preview">
                    {conv.lastMessage?.content || 'Chưa có tin nhắn'}
                  </div>
                </div>
              )
            })}
            
            {conversations.length === 0 && (
              <div style={{ padding: '20px', textAlign: 'center', color: '#999' }}>
                Chưa có cuộc trò chuyện nào
              </div>
            )}
          </div>
        </>
      )}

      {activeTab === 'users' && (
        <>
          <div style={{ padding: '10px 20px', borderBottom: '1px solid #e0e0e0' }}>
            <input
              type="text"
              placeholder="🔍 Tìm kiếm người dùng..."
              value={searchTerm}
              onChange={(e) => setSearchTerm(e.target.value)}
              style={{
                width: '100%',
                padding: '10px',
                border: '1px solid #ddd',
                borderRadius: '5px',
                fontSize: '14px'
              }}
            />
          </div>
          
          <div className="conversation-list">
            {loadingUsers ? (
              <div style={{ padding: '20px', textAlign: 'center', color: '#999' }}>
                Đang tải...
              </div>
            ) : users.length === 0 ? (
              <div style={{ padding: '20px', textAlign: 'center', color: '#999' }}>
                {searchTerm ? 'Không tìm thấy người dùng' : 'Không có người dùng nào'}
              </div>
            ) : (
              users.map((user) => (
                <div
                  key={user.id}
                  className="conversation-item"
                  onClick={() => handleUserClick(user)}
                >
                  <div className="conversation-name">
                    <span>{user.fullName || user.username || user.email}</span>
                  </div>
                  <div className="conversation-preview">
                    {user.email}
                  </div>
                </div>
              ))
            )}
          </div>
        </>
      )}

      {showNewConversationModal && (
        <NewConversationModal
          onClose={() => setShowNewConversationModal(false)}
          onConversationCreated={handleConversationCreated}
          currentUserId={currentUserId}
        />
      )}
    </div>
  )
}

export default Sidebar
