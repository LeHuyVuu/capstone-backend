import axios from 'axios'
import { authService } from './authService'

const API_URL = '/api/messaging'

class ChatService {
  async getConversations() {
    const response = await axios.get(`${API_URL}/conversations`, {
      headers: authService.getAuthHeader()
    })
    const data = response.data.data || response.data
    return Array.isArray(data) ? data : []
  }

  async getMessages(conversationId, pageNumber = 1, pageSize = 50) {
    const response = await axios.get(
      `${API_URL}/conversations/${Number(conversationId)}/messages`,
      {
        params: { pageNumber, pageSize },
        headers: authService.getAuthHeader()
      }
    )
    return response.data.data || response.data || {}
  }

  async sendMessage(conversationId, content, messageType = 0, referenceId = null, referenceType = null) {
    const response = await axios.post(
      `${API_URL}/messages`,
      {
        conversationId: Number(conversationId),
        content,
        messageType: Number(messageType),
        referenceId: referenceId ? Number(referenceId) : null,
        referenceType
      },
      {
        headers: authService.getAuthHeader()
      }
    )
    return response.data.data || response.data
  }

  async createConversation(name, memberIds, type = 1) {
    const response = await axios.post(
      `${API_URL}/conversations`,
      {
        name,
        memberIds,
        type
      },
      {
        headers: authService.getAuthHeader()
      }
    )
    return response.data.data || response.data
  }

  async getOrCreateDirectConversation(otherUserId) {
    // Ensure otherUserId is a number
    const userId = Number(otherUserId)
    if (isNaN(userId)) {
      throw new Error('Invalid user ID')
    }
    
    const response = await axios.post(
      `${API_URL}/conversations/direct/${userId}`,
      {},
      {
        headers: authService.getAuthHeader()
      }
    )
    console.log('getOrCreateDirectConversation response:', response.data)
    
    // Handle different response formats
    const conversation = response.data.data || response.data
    if (!conversation || !conversation.id) {
      console.error('Invalid conversation response:', response.data)
      throw new Error('Server returned invalid conversation data')
    }
    
    return conversation
  }

  async addMembers(conversationId, memberIds) {
    await axios.post(
      `${API_URL}/conversations/${conversationId}/members`,
      memberIds,
      {
        headers: authService.getAuthHeader()
      }
    )
  }
}

export const chatService = new ChatService()
