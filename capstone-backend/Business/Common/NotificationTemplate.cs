namespace capstone_backend.Business.Common
{
    public static class NotificationTemplate
    {
        public static class DatePlan
        {
            public const string TitleReminderPrimary = "Sắp tới buổi hẹn của bạn 💕";
            public const string TitleReminderSecondary = "Đến giờ chuẩn bị đi hẹn rồi ⏰";

            public const string TitleDatePlanStarted = "Date time! Bắt đầu";
            public const string TitelDatePlanEnded = "Buổi hẹn đã kết thúc";
            public const string TitleDatePlanSoftEnded = "Buổi hẹn dự kiến đã kết thúc";

            public const string TitleDatePlanAutoClosed = "Buổi hẹn đã được đóng tự động";

            public const string TitleAccepted = "Buổi hẹn đã được đồng ý!";
            public const string TitleDatePlanCancelled = "Buổi hẹn đã bị hủy";
            public const string TitleDatePlanRejected = "Lịch trình đã bị từ chối";

            public const string TitleDatePlanCompleted = "Hoàn thành buổi hẹn";

            public static string GetReminderPrimaryBody(string datePlanTitle, TimeOnly plannedStartAt)
            {
                return $"Buổi hẹn \"{datePlanTitle}\" sẽ bắt đầu lúc {plannedStartAt:HH:mm}. Mình chuẩn bị nhé!";
            }

            public static string GetReminderSecondaryBody(string datePlanTitle, TimeOnly plannedStartAt)
            {
                return $"Nhắc nhẹ: \"{datePlanTitle}\" sắp tới giờ ({plannedStartAt:HH:mm}) rồi nè!";
            }

            public static string GetDatePlanStartedBody(string datePlanTitle)
            {
                return $"Buổi hẹn \"{datePlanTitle}\" của chúng ta đã bắt đầu rồi đấy! Cùng tận hưởng nhé!";
            }

            public static string GetDatePlanEndedBody(string datePlanTitle)
            {
                return $"Buổi hẹn \"{datePlanTitle}\" của chúng ta đã kết thúc. Hy vọng bạn đã có những khoảnh khắc tuyệt vời!";
            }

            public static string GetDatePlanCompletedBody(string datePlanTitle)
            {
                return $"Buổi hẹn \"{datePlanTitle}\" của chúng ta đã hoàn thành! Hãy nhớ đánh giá địa điểm và chia sẻ cảm nhận nhé!";
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

            public static string GetDatePlanCancelledBody(string datePlanTitle)
            {
                return $"Buổi hẹn \"{datePlanTitle}\" của bạn đã bị hủy. Hãy lên kế hoạch cho buổi hẹn tiếp theo nhé!";
            }

            public static string GetDatePlanRejectedBody(string datePlanTitle)
            {
                return $"Lịch trình \"{datePlanTitle}\" partner đã từ chối. Đừng nản lòng, hãy lên kế hoạch cho buổi hẹn tiếp theo nhé!";
            }
        }

        public static class Review
        {
            public const string TitleReviewRequest = "⏳ Đến giờ đánh giá rồi!";
            public const string TitleReceiveNewReview = "Một đánh giá mới";
            public const string TitleReviewFlagged = "Đánh giá của bạn đang được xem xét";

            public static string GetReviewRequestBody(string venueName)
            {
                return $"Bạn vẫn đang ở 📍{venueName}📍 chứ? Cùng Đánh giá ngay nào!";
            }

            public static string GetReceiveNewReviewBody(string fullName, string venueName)
            {
                return $"{fullName} vừa đánh giá địa điểm {venueName} của bạn!";
            }

            public static string GetReviewFlaggedBody(string venueName)
            {
                return $"Đánh giá của bạn tại {venueName} đang được hệ thống kiểm duyệt thêm. Chúng tôi sẽ cập nhật sớm.";
            }
        }

        public static class Post
        {
            public const string TitleNewLike = "Có đồng minh vừa thích bài viết của bạn!";
            public const string TitleNewComment = "Có đồng minh vừa bình luận về bài viết của bạn!";
            public const string TitleNewCommentReply = "Có đồng minh vừa trả lời bình luận của bạn!";

            public const string TitleNewLikeComment = "Có đồng minh vừa thích bình luận của bạn!";

            public const string TitlePostFlagged = "Bài viết của bạn đang được xem xét";
            public const string TitleCommentFlagged = "Bình luận của bạn đang được xem xét";

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

            public static string GetPostFlaggedBody()
            {
                return $"Bài viết của bạn đang được hệ thống kiểm duyệt thêm. Chúng tôi sẽ cập nhật sớm.";
            }

            public static string GetCommentFlaggedBody()
            {
                return $"Bình luận của bạn đang được hệ thống kiểm duyệt thêm. Chúng tôi sẽ cập nhật sớm.";
            }
        }

        public static class Challenge
        {
            public const string TitleNewChallenge = "Bạn đã tham gia một thử thách mới!";
            public const string TitleChallengeCompleted = "Bạn đã hoàn thành thử thách!";

            public const string TitlePartnerNewChallenge = "Partner của bạn vừa tham gia một thử thách!";

            public const string TitleChallengeProgress = "Tiến độ thử thách của bạn đã được cập nhật!";

            public const string TitleCompleteChallengeSoon = "Thử thách hoàn thành sớm! 🏆";

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

            public static string GetChallengeProgressBody(string challengeTitle, int current, int target)
            {
                return $"Tiến độ thử thách \"{challengeTitle}\" của bạn đã được cập nhật! Bạn đã hoàn thành {current}/{target} yêu cầu thử thách rồi đấy!";
            }

            public static string GetPartnerChallengeProgressBody(string partnerName, string challengeTitle, int current, int target)
            {
                return $"Tiến độ thử thách \"{challengeTitle}\" đã được cập nhật! {partnerName} đã tăng tiến độ lên {current}/{target}";
            }

            public static string GetCompleteChallengeSoonBody(string challengeTitle)
            {
                return $"Do hệ thống cập nhật lại các địa điểm, cặp đôi của bạn đã về đích sớm trong thử thách [{challengeTitle}]. Check phần thưởng ngay thôi!";
            }
        }

        public static class Voucher
        {
            public const string TitleRefundInactiveVoucher = "Hoàn tiền voucher";
            public static string GetRefundInactiveVoucherBody(string voucherTitle, string points)
            {
                return $"Voucher {voucherTitle} đã bị hủy do tất cả địa điểm liên kết đều ngưng hoạt động. Số điểm : {points} đã được hoàn lại vào ví của bạn.";
            }
        }
    }
}
