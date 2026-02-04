
using capstone_backend.Business.Interfaces;
using capstone_backend.Data.Enums;
using Hangfire;
using Microsoft.EntityFrameworkCore;

namespace capstone_backend.Business.Jobs.DatePlan
{    
    public class DatePlanWorker : IDatePlanWorker
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<DatePlanWorker> _logger;

        public DatePlanWorker(IUnitOfWork unitOfWork, ILogger<DatePlanWorker> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        [JobDisplayName("End DatePlan #{0}")]
        public async Task EndDatePlanAsync(int datePlanId)
        {
            var plan = await _unitOfWork.DatePlans.GetByIdAsync(datePlanId);

            if (plan != null && plan.Status == DatePlanStatus.IN_PROGRESS.ToString())
            {
                _logger.LogInformation($"[END] Ending DatePlan #{datePlanId}");

                plan.Status = DatePlanStatus.COMPLETED.ToString();

                // todo: notify users

                await CleanupJobAsync(datePlanId, DatePlanJobType.END.ToString());

                await _unitOfWork.SaveChangesAsync();
            }
        }

        [JobDisplayName("Start DatePlan #{0}")]
        public async Task StartDatePlanAsync(int datePlanId)
        {
            var plan = await _unitOfWork.DatePlans.GetByIdAsync(datePlanId);

            if (plan != null && plan.Status == DatePlanStatus.SCHEDULED.ToString())
            {
                _logger.LogInformation($"[START] Starting DatePlan #{datePlanId}");

                plan.Status = DatePlanStatus.IN_PROGRESS.ToString();

                // todo: notify users

                await CleanupJobAsync(datePlanId, DatePlanJobType.START.ToString());

                await _unitOfWork.SaveChangesAsync();
            }
        }

        // Clean up done date plans
        private async Task CleanupJobAsync(int datePlanId, string jobType)
        {
            var job = await _unitOfWork.DatePlanJobs.GetByDatePlanIdAndJobType(datePlanId, jobType);

            if (job != null)
            {
                _unitOfWork.DatePlanJobs.Delete(job);
                await _unitOfWork.SaveChangesAsync();
            }
        }
    }
}
