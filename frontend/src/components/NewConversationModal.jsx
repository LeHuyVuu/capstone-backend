import { useState, useEffect } from 'react'
import { userService } from '../services/userService'
import { chatService } from '../services/chatService'

function NewConversationModal({ onClose, onConversationCreated, currentUserId }) {
  const [users, setUsers] = useState([])
  const [selectedUsers, setSelectedUsers] = useState([])
  const [conversationName, setConversationName] = useState('')
  const [loading, setLoading] = useState(true)
  const [creating, setCreating] = useState(false)

  useEffect(() => {
    loadUsers()
  }, [])

  const loadUsers = async () => {
    try {
      const data = await userService.getAllUsers()
      // Filter out current user
      const filteredUsers = data.filter(u => u.id !== currentUserId)
      setUsers(filteredUsers)
    } catch (error) {
      console.error('Error loading users:', error)
    } finally {
      setLoading(false)
    }
  }

  const toggleUser = (userId) => {
    if (selectedUsers.includes(userId)) {
      setSelectedUsers(selectedUsers.filter(id => id !== userId))
    } else {
      setSelectedUsers([...selectedUsers, userId])
    }
  }

  const handleCreate = async () => {
    if (selectedUsers.length === 0) return

    setCreating(true)
    try {
      let conversation
      
      if (selectedUsers.length === 1) {
        // Direct conversation - ensure userId is a number
        conversation = await chatService.getOrCreateDirectConversation(Number(selectedUsers[0]))
      } else {
        // Group conversation - ensure all IDs are numbers
        conversation = await chatService.createConversation(
          conversationName || 'Nhóm chat',
          selectedUsers.map(id => Number(id)),
          1 // Group type
        )
      }
      
      onConversationCreated(conversation)
      onClose()
    } catch (error) {
      console.error('Error creating conversation:', error)
      alert('Không thể tạo cuộc trò chuyện. Vui lòng thử lại.')
    } finally {
      setCreating(false)
    }
  }

  return (
    <div className="modal-overlay" onClick={onClose}>
      <div className="modal" onClick={(e) => e.stopPropagation()}>
        <h3>Tạo cuộc trò chuyện mới</h3>
        
        {loading ? (
          <div>Đang tải...</div>
        ) : (
          <>
            {selectedUsers.length > 1 && (
              <div className="form-group" style={{ marginBottom: '20px' }}>
                <label>Tên nhóm</label>
                <input
                  type="text"
                  value={conversationName}
                  onChange={(e) => setConversationName(e.target.value)}
                  placeholder="Nhập tên nhóm..."
                  style={{ 
                    width: '100%', 
                    padding: '10px', 
                    borderRadius: '5px',
                    border: '1px solid #ddd'
                  }}
                />
              </div>
            )}

            <div style={{ marginBottom: '10px', fontWeight: 600 }}>
              Chọn người để chat ({selectedUsers.length} người)
            </div>
            
            <div style={{ maxHeight: '400px', overflowY: 'auto' }}>
              {users.length === 0 ? (
                <div>Không có người dùng nào</div>
              ) : (
                users.map((user) => (
                  <div
                    key={user.id}
                    className={`date-plan-item ${selectedUsers.includes(user.id) ? 'selected' : ''}`}
                    onClick={() => toggleUser(user.id)}
                    style={{ cursor: 'pointer' }}
                  >
                    <div style={{ display: 'flex', alignItems: 'center', gap: '10px' }}>
                      <input
                        type="checkbox"
                        checked={selectedUsers.includes(user.id)}
                        onChange={() => {}}
                        style={{ cursor: 'pointer' }}
                      />
                      <div>
                        <div style={{ fontWeight: 600 }}>
                          {user.fullName || user.username || user.email}
                        </div>
                        <div style={{ fontSize: '12px', color: '#666' }}>
                          {user.email}
                        </div>
                      </div>
                    </div>
                  </div>
                ))
              )}
            </div>
          </>
        )}

        <div className="modal-actions">
          <button className="btn-secondary" onClick={onClose} disabled={creating}>
            Hủy
          </button>
          <button 
            className="btn" 
            onClick={handleCreate}
            disabled={selectedUsers.length === 0 || creating}
          >
            {creating ? 'Đang tạo...' : 'Tạo cuộc trò chuyện'}
          </button>
        </div>
      </div>
    </div>
  )
}

export default NewConversationModal
