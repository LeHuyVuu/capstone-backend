namespace capstone_backend.Business.Common
{
    public static class NotificationTemplate
    {
        public static class DatePlan
        {
            public const string TitleReminder1Day = "Mai mình có hẹn nha bồ ơi!";
            public const string TitleReminder1Hour = "Còn 1 giờ nữa tới hẹn rồi";

            public const string TitleDatePlanStarted = "Date time! Bắt đầu";
            public const string TitelDatePlanEnded = "Buổi hẹn đã kết thúc";
            public const string TitleDatePlanSoftEnded = "Buổi hẹn dự kiến đã kết thúc";

            public const string TitleDatePlanAutoClosed = "Buổi hẹn đã được đóng tự động";

            public static string GetReminder1DayBody(string datePlanTitle, TimeOnly plannedStartAt)
            {
                return $"Đừng quên là ngày mai mình có hẹn \"{datePlanTitle}\" vào lúc {plannedStartAt:HH:mm} nhé!";
            }

            public static string GetReminder1HourBody(string datePlanTitle, TimeOnly plannedStartAt)
            {
                return $"Chỉ còn 1 tiếng nữa là đến giờ hẹn \"{datePlanTitle}\" vào lúc {plannedStartAt:HH:mm} rồi đấy!";
            }

            public static string GetDatePlanStartedBody(string datePlanTitle)
            {
                return $"Buổi hẹn \"{datePlanTitle}\" của chúng ta đã bắt đầu rồi đấy! Cùng tận hưởng nhé!";
            }

            public static string GetDatePlanEndedBody(string datePlanTitle)
            {
                return $"Buổi hẹn \"{datePlanTitle}\" của chúng ta đã kết thúc. Hy vọng bạn đã có những khoảnh khắc tuyệt vời!";
            }

            public static string GetDatePlanSoftEndedBody(string datePlanTitle)
            {
                return $"Buổi hẹn \"{datePlanTitle}\" của chúng ta đã kết thúc theo dự kiến. Hãy nhớ cập nhật trạng thái nhé!";
            }
            public static string GetDatePlanAutoClosedBody(string datePlanTitle)
            {
                return $"Buổi hẹn \"{datePlanTitle}\" đã được đóng tự động. Hãy lên kế hoạch cho buổi hẹn tiếp theo nhé!";
            }
        }

        public static class Review
        {
            public const string TitleReviewRequest = "⏳ Đã 10 phút rồi!";
            public static string GetReviewRequestBody(string venueName)
            {
                return $"Bạn vẫn đang ở 📍{venueName}📍 chứ? Cùng Đánh giá ngay nào!";
            }
        }
    }
}
