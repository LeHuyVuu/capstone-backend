import { useState, useEffect, useRef } from 'react'
import MessageList from './MessageList'
import MessageInput from './MessageInput'
import DatePlanModal from './DatePlanModal'

function ChatArea({ conversation, messages, onSendMessage, currentUser }) {
  const [showDatePlanModal, setShowDatePlanModal] = useState(false)

  if (!conversation) {
    return (
      <div className="chat-area">
        <div className="empty-chat">
          Chọn một cuộc trò chuyện để bắt đầu
        </div>
      </div>
    )
  }

  const handleShareDatePlan = (datePlan) => {
    onSendMessage(
      `Đã chia sẻ kế hoạch: ${datePlan.title || 'Date Plan'}`,
      1, // MessageType.SYSTEM hoặc custom type
      datePlan.id,
      'DatePlan'
    )
    setShowDatePlanModal(false)
  }

  // Get display name for conversation
  let displayName = conversation.name || 'Cuộc trò chuyện'
  if (conversation.type === 'DIRECT' && conversation.members && currentUser) {
    const otherMember = conversation.members.find(m => m.userId !== currentUser.id)
    if (otherMember) {
      displayName = otherMember.fullName || otherMember.username || displayName
    }
  }

  return (
    <div className="chat-area">
      <div className="chat-header">
        <h3>{displayName}</h3>
        <div className="header-actions">
          <button 
            className="icon-btn"
            onClick={() => setShowDatePlanModal(true)}
          >
            Chia sẻ Date Plan
          </button>
        </div>
      </div>

      <MessageList messages={messages} currentUser={currentUser} />
      
      <MessageInput 
        conversationId={conversation.id}
        onSendMessage={onSendMessage} 
      />

      {showDatePlanModal && (
        <DatePlanModal
          onClose={() => setShowDatePlanModal(false)}
          onSelectDatePlan={handleShareDatePlan}
        />
      )}
    </div>
  )
}

export default ChatArea
