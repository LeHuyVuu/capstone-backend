using capstone_backend.Business.DTOs.Challenge;
using capstone_backend.Data.Enums;
using capstone_backend.Extensions.Common;
using Microsoft.IdentityModel.Tokens;

namespace capstone_backend.Business.Common.Helpers
{
    public static class ChallengeProgressExtraBuilder
    {
        public static CoupleChallengeProgressExtraResponse? Build(CoupleChallengeProgressData? progress, int currentMemberId)
        {
            if (progress == null || string.IsNullOrWhiteSpace(progress.Trigger))
                return null;

            if (string.Equals(progress.Trigger, ChallengeTriggerEvent.CHECKIN.ToString(), StringComparison.OrdinalIgnoreCase))
                return BuildCheckinExtra(progress, currentMemberId);

            if (string.Equals(progress.Trigger, ChallengeTriggerEvent.REVIEW.ToString(), StringComparison.OrdinalIgnoreCase))
                return BuildReviewExtra(progress);

            if (string.Equals(progress.Trigger, ChallengeTriggerEvent.POST.ToString(), StringComparison.OrdinalIgnoreCase))
                return BuildPostExtra(progress);

            return null;
        }

        private static CheckinChallengeProgressExtraResponse BuildCheckinExtra(CoupleChallengeProgressData progress, int currentMemberId)
        {
            // turn time tot VN time
            var now = DateTime.UtcNow;
            var nowVn = TimezoneUtil.ToVietNamTime(now);
            var today = DateOnly.FromDateTime(nowVn);

            var monthKey = $"{today:yyyy-MM}";
            var day = today.Day;
            var memberKey = currentMemberId.ToString();

            var totalMembers = progress.MemberState?.Count ?? progress.Members?.Count ?? 0;
            var doneMembersToday = 0;
            var currentMemberCheckedInToday = false;

            if (progress.DailyHistory?.Months != null &&
                progress.DailyHistory.Months.TryGetValue(monthKey, out var memberMap) &&
                memberMap != null
            )
            {
                foreach (var kv in memberMap)
                    if (IsDayChecked(kv.Value, day))
                        doneMembersToday++;

                if (memberMap.TryGetValue(memberKey, out var currentMemberMask))
                    currentMemberCheckedInToday = IsDayChecked(currentMemberMask, day);
            }

            return new CheckinChallengeProgressExtraResponse
            {
                Type = ChallengeTriggerEvent.CHECKIN.ToString(),
                Date = today,
                CanCheckinToday = !currentMemberCheckedInToday,
                DoneMembersToday = doneMembersToday,
                TotalMembers = totalMembers,
                MemberCurrentStreak = progress.Members[memberKey].Current,
                MemberLongestStreak = progress.Members[memberKey].Streak,
                CoupleCurrentStreak = progress.Streak.Current,
                CoupleLongestStreak = progress.Streak.Best
            };
        }

        private static bool IsDayChecked(int monthMask, int day)
        {
            if (day <= 0 || day > 31)
                return false;

            var bitIndex = day - 1;
            return (monthMask & (1 << bitIndex)) != 0;
        }

        private static ReviewChallengeProgressExtraResponse BuildReviewExtra(CoupleChallengeProgressData progress)
        {
            var qualifiedCount = progress.Current;
            DateTime? lastQualifiedReviewAt = null;
            int? lastVenueId = null;
            string? lastVenueName = null;

            var orderedQualifiedItems = progress.QualifiedItems?
                .OrderByDescending(item => item.ActionAt);

            if (orderedQualifiedItems != null)
            {
                var lastQualifiedItem = orderedQualifiedItems.FirstOrDefault();
                if (lastQualifiedItem != null)
                {
                    lastQualifiedReviewAt = lastQualifiedItem.ActionAt;
                    lastVenueId = lastQualifiedItem.RefId;
                    lastVenueName = lastQualifiedItem.Name;
                }
            }

            return new ReviewChallengeProgressExtraResponse
            {
                Type = ChallengeTriggerEvent.REVIEW.ToString(),
                QualifiedReviewCount = qualifiedCount,
                LastQualifiedReviewAt = lastQualifiedReviewAt,
                LastVenueId = lastVenueId,
                LastVenueName = lastVenueName
            };
        }
    }
}
