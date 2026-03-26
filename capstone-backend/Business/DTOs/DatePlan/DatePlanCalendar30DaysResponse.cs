namespace capstone_backend.Business.DTOs.DatePlan
{
    public class DatePlanCalendar30DaysResponse
    {
        public DateOnly StartDay { get; set; }
        public DateOnly EndDay { get; set; }
        public List<DatePlanCalendarDayItemResponse> Days { get; set; } = new List<DatePlanCalendarDayItemResponse>();
    }

    public class DatePlanCalendarDayItemResponse
    {
        public DateOnly Date { get; set; }
        public bool HasDatePlan { get; set; }
        public List<int> DatePlanIds { get; set; } = new List<int>();
    }
}
