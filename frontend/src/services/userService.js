import axios from 'axios'
import { authService } from './authService'

const API_URL = '/api/users'

class UserService {
  async getAllUsers(pageSize = 100, searchTerm = '') {
    try {
      const response = await axios.get(API_URL, {
        params: {
          pageNumber: 1,
          pageSize: pageSize,
          searchTerm: searchTerm
        },
        headers: authService.getAuthHeader()
      })
      
      // Backend trả về format: { data: { items: [...], ... } }
      return response.data.data?.items || response.data.data || []
    } catch (error) {
      console.error('Error fetching users:', error)
      throw error
    }
  }

  async getUserById(userId) {
    try {
      const response = await axios.get(`${API_URL}/${userId}`, {
        headers: authService.getAuthHeader()
      })
      return response.data.data
    } catch (error) {
      console.error('Error fetching user:', error)
      throw error
    }
  }

  async searchUsers(searchTerm, pageSize = 50) {
    return this.getAllUsers(pageSize, searchTerm)
  }
}

export const userService = new UserService()
