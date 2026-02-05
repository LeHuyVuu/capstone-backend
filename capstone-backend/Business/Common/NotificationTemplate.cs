namespace capstone_backend.Business.Common
{
    public static class NotificationTemplate
    {
        public static class DatePlan
        {
            public const string TitleReminder1Day = "Mai mình có hẹn nha bồ ơi!";
            public const string TitleReminder1Hour = "Còn 1 giờ nữa tới hẹn rồi";

            public static string GetReminder1DayBody(string datePlanTitle, TimeOnly plannedStartAt)
            {
                return $"Đừng quên là ngày mai mình có hẹn \"{datePlanTitle}\" vào lúc {plannedStartAt:HH:mm} nhé!";
            }

            public static string GetReminder1HourBody(string datePlanTitle, TimeOnly plannedStartAt)
            {
                return $"Chỉ còn 1 tiếng nữa là đến giờ hẹn \"{datePlanTitle}\" vào lúc {plannedStartAt:HH:mm} rồi đấy!";
            }
        }
    }
}
