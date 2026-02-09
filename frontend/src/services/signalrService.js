import * as signalR from '@microsoft/signalr'
import { authService } from './authService'

class SignalRService {
  constructor() {
    this.connection = null
    this.callbacks = {
      onMessageReceived: null,
      onTyping: null,
      onUserOnline: null,
      onUserOffline: null
    }
  }

  async connect() {
    const token = authService.getToken()
    
    if (!token) {
      console.warn('No auth token found, skipping SignalR connection')
      return false
    }

    try {
      this.connection = new signalR.HubConnectionBuilder()
        .withUrl('/hubs/messaging', {
          accessTokenFactory: () => token,
          skipNegotiation: false,
          transport: signalR.HttpTransportType.LongPolling // Force LongPolling to avoid WebSocket issues
        })
        .withAutomaticReconnect()
        .configureLogging(signalR.LogLevel.Information)
        .build()

      // Setup event handlers
      this.connection.on('ReceiveMessage', (message) => {
        if (this.callbacks.onMessageReceived) {
          this.callbacks.onMessageReceived(message)
        }
      })

      this.connection.on('UserTyping', (data) => {
        if (this.callbacks.onTyping) {
          this.callbacks.onTyping(data)
        }
      })

      this.connection.on('UserOnline', (userId) => {
        if (this.callbacks.onUserOnline) {
          this.callbacks.onUserOnline(userId)
        }
      })

      this.connection.on('UserOffline', (userId, lastSeen) => {
        if (this.callbacks.onUserOffline) {
          this.callbacks.onUserOffline(userId, lastSeen)
        }
      })

      await this.connection.start()
      console.log('✅ SignalR Connected successfully')
      return true
    } catch (err) {
      console.error('❌ SignalR Connection Error:', err.message || err)
      console.warn('App will continue without realtime features. Make sure backend is running.')
      return false
    }
  }

  async disconnect() {
    if (this.connection) {
      await this.connection.stop()
      this.connection = null
    }
  }

  async joinConversation(conversationId) {
    if (this.connection?.state === signalR.HubConnectionState.Connected) {
      try {
        await this.connection.invoke('JoinConversation', conversationId)
      } catch (err) {
        console.warn('Failed to join conversation:', err.message)
      }
    }
  }

  async leaveConversation(conversationId) {
    if (this.connection?.state === signalR.HubConnectionState.Connected) {
      try {
        await this.connection.invoke('LeaveConversation', conversationId)
      } catch (err) {
        console.warn('Failed to leave conversation:', err.message)
      }
    }
  }

  async sendTypingIndicator(conversationId, isTyping) {
    if (this.connection?.state === signalR.HubConnectionState.Connected) {
      try {
        await this.connection.invoke('SendTypingIndicator', conversationId, isTyping)
      } catch (err) {
        console.warn('Failed to send typing indicator:', err.message)
      }
    }
  }

  onMessageReceived(callback) {
    this.callbacks.onMessageReceived = callback
  }

  onTyping(callback) {
    this.callbacks.onTyping = callback
  }

  onUserOnline(callback) {
    this.callbacks.onUserOnline = callback
  }

  onUserOffline(callback) {
    this.callbacks.onUserOffline = callback
  }
}

export const signalrService = new SignalRService()
