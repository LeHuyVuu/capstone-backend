import axios from 'axios'
import { authService } from './authService'

const API_URL = '/api/dateplan'

class DatePlanService {
  async getDatePlans() {
    const response = await axios.get(API_URL, {
      headers: authService.getAuthHeader()
    })
    return response.data.data || []
  }

  async getDatePlan(datePlanId) {
    const response = await axios.get(`${API_URL}/${datePlanId}`, {
      headers: authService.getAuthHeader()
    })
    return response.data.data
  }
}

export const datePlanService = new DatePlanService()
