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

            public const string TitleAccepted = "Buổi hẹn đã được đồng ý!";

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

            public static string GetAcceptedBody(string datePlanTitle)
            {
                return $"Buổi hẹn \"{datePlanTitle}\" của bạn đã được partner đồng ý! Hãy chuẩn bị cho một buổi hẹn tuyệt vời nhé!";
            }
        }

        public static class Review
        {
            public const string TitleReviewRequest = "⏳ Đã 10 phút rồi!";
            public const string TitleReceiveNewReview = "Một đánh giá mới";

            public static string GetReviewRequestBody(string venueName)
            {
                return $"Bạn vẫn đang ở 📍{venueName}📍 chứ? Cùng Đánh giá ngay nào!";
            }

            public static string GetReceiveNewReviewBody(string fullName, string venueName)
            {
                return $"{fullName} vừa đánh giá địa điểm {venueName} của bạn!";
            }
        }

        public static class Post
        {
            public const string TitleNewLike = "Có đồng minh vừa thích bài viết của bạn!";
            public const string TitleNewComment = "Có đồng minh vừa bình luận về bài viết của bạn!";
            public const string TitleNewCommentReply = "Có đồng minh vừa trả lời bình luận của bạn!";

            public const string TitleNewLikeComment = "Có đồng minh vừa thích bình luận của bạn!";

            public static string GetNewLikeBody(string fullName)
            {
                return $"{fullName} vừa thích bài viết của bạn!";
            }

            public static string GetNewCommentBody(string fullName)
            {
                return $"{fullName} vừa bình luận về bài viết của bạn!";
            }

            public static string GetNewCommentReplyBody(string fullName)
            {
                return $"{fullName} vừa trả lời bình luận của bạn!";
            }

            public static string GetNewLikeCommentBody(string fullName)
            {
                return $"{fullName} vừa thích bình luận của bạn!";
            }
        }

        public static class Challenge
        {
            public const string TitleNewChallenge = "Bạn đã tham gia một thử thách mới!";
            public const string TitleChallengeCompleted = "Bạn đã hoàn thành thử thách!";

            public const string TitlePartnerNewChallenge = "Partner của bạn vừa tham gia một thử thách!";

            public static string GetNewChallengeBody(string challengeTitle)
            {
                return $"Bạn đã tham gia thử thách \"{challengeTitle}\"! Hãy hoàn thành thử thách để nhận phần thưởng nhé!";
            }

            public static string GetChallengeCompletedBody(string challengeTitle)
            {
                return $"Chúc mừng bạn đã hoàn thành thử thách \"{challengeTitle}\"! Hãy nhận phần thưởng của bạn nhé!";
            }

            public static string GetPartnerNewChallengeBody(string partnerName, string challengeTitle)
            {
                return $"{partnerName} vừa tham gia thử thách \"{challengeTitle}\"! Tham gia chứ?";
            }
        }
    }
}
