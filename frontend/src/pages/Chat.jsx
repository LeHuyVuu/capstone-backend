import { useState, useEffect, useRef } from 'react'
import Sidebar from '../components/Sidebar'
import ChatArea from '../components/ChatArea'
import { chatService } from '../services/chatService'
import { signalrService } from '../services/signalrService'
import { authService } from '../services/authService'

function Chat({ onLogout }) {
  const [conversations, setConversations] = useState([])
  const [selectedConversation, setSelectedConversation] = useState(null)
  const [messages, setMessages] = useState([])
  const [loading, setLoading] = useState(true)
  const currentUser = authService.getUser()
  const selectedConversationRef = useRef(null)

  // Debug currentUser
  console.log('Chat currentUser:', currentUser)

  // Keep ref in sync with state
  useEffect(() => {
    selectedConversationRef.current = selectedConversation
  }, [selectedConversation])

  useEffect(() => {
    loadConversations()
    setupSignalR()

    return () => {
      signalrService.disconnect()
    }
  }, [])

  const setupSignalR = async () => {
    const connected = await signalrService.connect()
    
    if (!connected) {
      console.warn('⚠️  Running in polling mode (no realtime updates)')
      console.warn('To enable realtime features, make sure backend is running at http://localhost:5000')
      return
    }

    signalrService.onMessageReceived((message) => {
      console.log('📨 New message received:', message)
      
      // Check if message belongs to currently selected conversation
      const currentConv = selectedConversationRef.current
      if (currentConv && message.conversationId === currentConv.id) {
        setMessages((prevMessages) => {
          // Check if message already exists (avoid duplicates)
          const exists = prevMessages.some(m => m.id === message.id)
          if (exists) {
            console.log('Message already exists, skipping')
            return prevMessages
          }
          
          // Add to end of list (newest at bottom)
          console.log('Adding message to list')
          return [...prevMessages, message]
        })
      }

      // Always update conversation list to show new last message
      loadConversations()
    })
  }

  const loadConversations = async () => {
    try {
      const data = await chatService.getConversations()
      setConversations(data)
      return data // Return data for chaining
    } catch (error) {
      console.error('Error loading conversations:', error)
      return []
    } finally {
      setLoading(false)
    }
  }

  const handleSelectConversation = async (conversation) => {
    if (!conversation || !conversation.id) {
      console.error('Invalid conversation:', conversation)
      return
    }
    
    console.log('Selecting conversation:', conversation.id)
    setSelectedConversation(conversation)
    
    // Leave old conversation
    if (selectedConversation && selectedConversation.id !== conversation.id) {
      await signalrService.leaveConversation(selectedConversation.id)
    }

    // Join new conversation
    await signalrService.joinConversation(conversation.id)

    // Load messages
    try {
      const data = await chatService.getMessages(conversation.id)
      const fetchedMessages = data.messages || []
      // Sort messages by createdAt to ensure correct order
      const sortedMessages = fetchedMessages.sort((a, b) => 
        new Date(a.createdAt) - new Date(b.createdAt)
      )
      setMessages(sortedMessages)
    } catch (error) {
      console.error('Error loading messages:', error)
      setMessages([])
    }
  }

  const handleSendMessage = async (content, messageType = 0, referenceId = null, referenceType = null) => {
    if (!selectedConversation) return

    try {
      const message = await chatService.sendMessage(
        selectedConversation.id,
        content,
        messageType,
        referenceId,
        referenceType
      )
      
      // Add message to end of list immediately
      setMessages((prev) => {
        // Check if message already exists (avoid duplicates from SignalR)
        const exists = prev.some(m => m.id === message.id)
        if (exists) return prev
        return [...prev, message]
      })
      
      // Reload conversations to update last message
      loadConversations()
    } catch (error) {
      console.error('Error sending message:', error)
      alert('Không thể gửi tin nhắn')
    }
  }

  return (
    <div className="chat-container">
      <Sidebar
        conversations={conversations}
        selectedConversation={selectedConversation}
        onSelectConversation={handleSelectConversation}
        onLogout={onLogout}
        onRefresh={loadConversations}
        currentUserId={currentUser?.id}
      />
      <ChatArea
        conversation={selectedConversation}
        messages={messages}
        onSendMessage={handleSendMessage}
        currentUser={currentUser}
      />
    </div>
  )
}

export default Chat
