import { useState } from 'react'
import { signalrService } from '../services/signalrService'

function MessageInput({ conversationId, onSendMessage }) {
  const [message, setMessage] = useState('')
  const [isTyping, setIsTyping] = useState(false)
  let typingTimeout = null

  const handleInputChange = (e) => {
    setMessage(e.target.value)
    
    // Send typing indicator
    if (!isTyping) {
      setIsTyping(true)
      signalrService.sendTypingIndicator(conversationId, true)
    }

    // Clear previous timeout
    if (typingTimeout) {
      clearTimeout(typingTimeout)
    }

    // Stop typing after 1 second of inactivity
    typingTimeout = setTimeout(() => {
      setIsTyping(false)
      signalrService.sendTypingIndicator(conversationId, false)
    }, 1000)
  }

  const handleSubmit = (e) => {
    e.preventDefault()
    
    if (message.trim()) {
      onSendMessage(message.trim())
      setMessage('')
      
      // Clear typing indicator
      if (isTyping) {
        setIsTyping(false)
        signalrService.sendTypingIndicator(conversationId, false)
      }
    }
  }

  const handleKeyPress = (e) => {
    if (e.key === 'Enter' && !e.shiftKey) {
      e.preventDefault()
      handleSubmit(e)
    }
  }

  return (
    <form className="message-input-container" onSubmit={handleSubmit}>
      <textarea
        className="message-input"
        value={message}
        onChange={handleInputChange}
        onKeyPress={handleKeyPress}
        placeholder="Nhập tin nhắn..."
        rows="1"
      />
      <button 
        type="submit" 
        className="send-btn"
        disabled={!message.trim()}
      >
        Gửi
      </button>
    </form>
  )
}

export default MessageInput
