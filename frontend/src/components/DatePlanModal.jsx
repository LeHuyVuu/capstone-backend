import { useState, useEffect } from 'react'
import { datePlanService } from '../services/datePlanService'

function DatePlanModal({ onClose, onSelectDatePlan }) {
  const [datePlans, setDatePlans] = useState([])
  const [selectedPlan, setSelectedPlan] = useState(null)
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    loadDatePlans()
  }, [])

  const loadDatePlans = async () => {
    try {
      const data = await datePlanService.getDatePlans()
      setDatePlans(data)
    } catch (error) {
      console.error('Error loading date plans:', error)
    } finally {
      setLoading(false)
    }
  }

  const handleShare = () => {
    if (selectedPlan) {
      onSelectDatePlan(selectedPlan)
    }
  }

  return (
    <div className="modal-overlay" onClick={onClose}>
      <div className="modal" onClick={(e) => e.stopPropagation()}>
        <h3>Chọn Date Plan để chia sẻ</h3>
        
        {loading ? (
          <div>Đang tải...</div>
        ) : datePlans.length === 0 ? (
          <div>Chưa có date plan nào</div>
        ) : (
          <div>
            {datePlans.map((plan) => (
              <div
                key={plan.id}
                className={`date-plan-item ${selectedPlan?.id === plan.id ? 'selected' : ''}`}
                onClick={() => setSelectedPlan(plan)}
              >
                <div style={{ fontWeight: 600, marginBottom: '5px' }}>
                  {plan.title || `Date Plan #${plan.id}`}
                </div>
                <div style={{ fontSize: '14px', color: '#666' }}>
                  {plan.description || 'Không có mô tả'}
                </div>
                <div style={{ fontSize: '12px', color: '#999', marginTop: '5px' }}>
                  {plan.startDate && new Date(plan.startDate).toLocaleDateString('vi-VN')}
                </div>
              </div>
            ))}
          </div>
        )}

        <div className="modal-actions">
          <button className="btn-secondary" onClick={onClose}>
            Hủy
          </button>
          <button 
            className="btn" 
            onClick={handleShare}
            disabled={!selectedPlan}
          >
            Chia sẻ
          </button>
        </div>
      </div>
    </div>
  )
}

export default DatePlanModal
