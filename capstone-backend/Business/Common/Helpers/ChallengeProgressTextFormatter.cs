using capstone_backend.Business.Common.Constants;
using capstone_backend.Business.DTOs.Challenge;
using capstone_backend.Data.Enums;

namespace capstone_backend.Business.Common.Helpers
{
    public static class ChallengeProgressTextFormatter
    {
        public static string Build(
            string trigger,
            string metric,
            int current,
            int target,
            bool isCompleted,
            CoupleChallengeProgressExtraResponse? progressExtra = null
        )
        {
            if (isCompleted)
                return "Hoàn thành thử thách";

            // Checkin
            if (string.Equals(trigger, ChallengeTriggerEvent.CHECKIN.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                if (progressExtra is CheckinChallengeProgressExtraResponse checkinExtra)
                    return $"{checkinExtra.DoneMembersToday}/{checkinExtra.TotalMembers} người đã điểm danh mood hôm nay";
            }

            return $"{current}/{target} điểm danh hợp lệ";

            // STREAK
            if (string.Equals(metric, ChallengeConstants.GoalMetrics.STREAK, StringComparison.OrdinalIgnoreCase))
            {
                if (progressExtra is CheckinChallengeProgressExtraResponse checkinExtra)
                {
                    return $"Chuỗi hiện tại: {checkinExtra.CurrentStreak} ngày";
                }

                return $"Chuỗi hiện tại: {current} ngày";
            }

            // Review/Post with unique venue list
            if (string.Equals(metric, ChallengeConstants.GoalMetrics.UNIQUE_LIST, StringComparison.OrdinalIgnoreCase))
            {
                if (string.Equals(trigger, ChallengeTriggerEvent.REVIEW.ToString(), StringComparison.OrdinalIgnoreCase))
                    return $"Đã review {current}/{target} địa điểm khác nhau";

                if (string.Equals(trigger, ChallengeTriggerEvent.POST.ToString(), StringComparison.OrdinalIgnoreCase))
                    return $"Đã đăng {current}/{target} mục tiêu khác nhau";

                return $"{current}/{target} mục tiêu duy nhất đã đạt";
            }

            if (string.Equals(metric, ChallengeConstants.GoalMetrics.COUNT, StringComparison.OrdinalIgnoreCase))
            {
                if (string.Equals(trigger, ChallengeTriggerEvent.REVIEW.ToString(), StringComparison.OrdinalIgnoreCase))
                    return $"{current}/{target} review hợp lệ";

                if (string.Equals(trigger, ChallengeTriggerEvent.POST.ToString(), StringComparison.OrdinalIgnoreCase))
                    return $"{current}/{target} bài đăng hợp lệ";

                if (string.Equals(trigger, ChallengeTriggerEvent.CHECKIN.ToString(), StringComparison.OrdinalIgnoreCase))
                    return $"{current}/{target} check-in hợp lệ";

                return $"{current}/{target} hoàn thành";
            }

            return $"{current}/{target} hoàn thành";
        }
    }
}
