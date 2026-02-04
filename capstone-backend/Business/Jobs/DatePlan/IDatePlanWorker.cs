namespace capstone_backend.Business.Jobs.DatePlan
{
    public interface IDatePlanWorker
    {
        Task StartDatePlanAsync(int datePlanId);
        Task EndDatePlanAsync(int datePlanId);
    }
}
