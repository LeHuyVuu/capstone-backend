import { useEffect, useRef } from 'react'

function MessageList({ messages, currentUser }) {
  const messagesEndRef = useRef(null)
  const messagesContainerRef = useRef(null)

  // Debug currentUser
  console.log('MessageList currentUser:', currentUser)

  const scrollToBottom = () => {
    if (messagesEndRef.current) {
      messagesEndRef.current.scrollIntoView({ behavior: 'smooth' })
    }
  }

  useEffect(() => {
    // Scroll to bottom whenever messages change
    scrollToBottom()
  }, [messages])

  // Also scroll immediately when component mounts or messages array length changes
  useEffect(() => {
    if (messagesContainerRef.current) {
      messagesContainerRef.current.scrollTop = messagesContainerRef.current.scrollHeight
    }
  }, [messages.length])

  const formatTime = (dateString) => {
    const date = new Date(dateString)
    return date.toLocaleTimeString('vi-VN', { 
      hour: '2-digit', 
      minute: '2-digit' 
    })
  }

  return (
    <div className="messages-container" ref={messagesContainerRef}>
      {messages.map((message) => {
        // Ensure both are numbers for comparison
        const messageSenderId = Number(message.senderId)
        const currentUserId = Number(currentUser?.id)
        const isMine = messageSenderId === currentUserId
        
        // Debug log
        if (message.id) {
          console.log(`Message ${message.id}: senderId=${messageSenderId}, currentUserId=${currentUserId}, isMine=${isMine}`)
        }

        return (
          <div key={message.id} className={`message ${isMine ? 'mine' : ''}`}>
            {!isMine && (
              <div className="message-sender">
                {message.senderName || 'Unknown'}
              </div>
            )}
            
            <div className="message-content">
              <div className="message-text">{message.content}</div>
              
              {message.referenceType === 'DatePlan' && message.referenceId && (
                <div className="date-plan-card">
                  📅 Kế hoạch hẹn hò #{message.referenceId}
                  <div style={{ fontSize: '12px', marginTop: '5px' }}>
                    Nhấn để xem chi tiết
                  </div>
                </div>
              )}
              
              <div className="message-time">
                {formatTime(message.createdAt)}
              </div>
            </div>
          </div>
        )
      })}
      <div ref={messagesEndRef} />
    </div>
  )
}

export default MessageList
