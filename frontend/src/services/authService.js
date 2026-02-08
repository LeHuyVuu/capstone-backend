import axios from 'axios'

const API_URL = '/api/auth'

class AuthService {
  async login(email, password) {
    const response = await axios.post(`${API_URL}/login`, {
      email,
      password,
      rememberMe: true
    })
    
    if (response.data.data?.accessToken) {
      const token = response.data.data.accessToken
      localStorage.setItem('token', token)
      
      // Decode JWT token to get user info
      try {
        const payload = JSON.parse(atob(token.split('.')[1]))
        const user = {
          id: parseInt(payload.sub), // JWT "sub" claim is user ID
          email: payload.email,
          fullName: payload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name'] || payload.name,
          role: payload['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'] || payload.role
        }
        localStorage.setItem('user', JSON.stringify(user))
        console.log('User from JWT:', user)
      } catch (error) {
        console.error('Error decoding JWT:', error)
      }
    }
    
    return response.data
  }

  logout() {
    localStorage.removeItem('token')
    localStorage.removeItem('user')
  }

  getToken() {
    return localStorage.getItem('token')
  }

  getUser() {
    const user = localStorage.getItem('user')
    if (!user || user === 'undefined' || user === 'null') {
      return null
    }
    try {
      return JSON.parse(user)
    } catch (error) {
      console.error('Error parsing user data:', error)
      return null
    }
  }

  getAuthHeader() {
    const token = this.getToken()
    return token ? { Authorization: `Bearer ${token}` } : {}
  }
}

export const authService = new AuthService()
