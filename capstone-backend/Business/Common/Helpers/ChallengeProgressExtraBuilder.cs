using capstone_backend.Business.DTOs.Challenge;
using capstone_backend.Data.Enums;
using capstone_backend.Extensions.Common;

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

        public static CoupleChallengeProgressExtraResponse BuildDetail(
            CoupleChallengeProgressData progress,
            int currentMemberId,
            Dictionary<int, string>? memberNameMap = null)
        {
            if (progress == null)
            {
                return new CoupleChallengeProgressExtraResponse
                {
                    Type = string.Empty
                };
            }

            if (string.Equals(progress.Trigger, ChallengeTriggerEvent.CHECKIN.ToString(), StringComparison.OrdinalIgnoreCase))
                return BuildCheckinExtra(progress, currentMemberId);

            if (string.Equals(progress.Trigger, ChallengeTriggerEvent.REVIEW.ToString(), StringComparison.OrdinalIgnoreCase))
                return BuildReviewDetailExtra(progress, memberNameMap);

            if (string.Equals(progress.Trigger, ChallengeTriggerEvent.POST.ToString(), StringComparison.OrdinalIgnoreCase))
                return BuildPostDetailExtra(progress, memberNameMap);

            return new CoupleChallengeProgressExtraResponse
            {
                Type = progress.Trigger ?? string.Empty
            };
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

            var memberCurrentStreak = 0;
            var memberLongestStreak = 0;

            if (progress.Members != null && progress.Members.TryGetValue(memberKey, out var memberProgress))
            {
                memberCurrentStreak = memberProgress.Current;
                memberLongestStreak = memberProgress.Streak;
            }

            return new CheckinChallengeProgressExtraResponse
            {
                Type = ChallengeTriggerEvent.CHECKIN.ToString(),
                Date = today,
                CanCheckinToday = !currentMemberCheckedInToday,
                DoneMembersToday = doneMembersToday,
                TotalMembers = totalMembers,
                MemberCurrentStreak = memberCurrentStreak,
                MemberLongestStreak = memberLongestStreak,
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
                    lastVenueId = lastQualifiedItem.VenueId;
                    lastVenueName = lastQualifiedItem.VenueName;
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

        private static PostChallengeProgressExtraResponse BuildPostExtra(CoupleChallengeProgressData progress)
        {
            var qualifiedCount = progress.Current;
            DateTime? lastQualifiedPostAt = null;
            int? lastPostId = null;

            var orderedQualifiedItems = progress.QualifiedItems?
                .OrderByDescending(item => item.ActionAt);

            if (orderedQualifiedItems != null)
            {
                var lastQualifiedItem = orderedQualifiedItems.FirstOrDefault();
                if (lastQualifiedItem != null)
                {
                    lastQualifiedPostAt = lastQualifiedItem.ActionAt;
                    lastPostId = lastQualifiedItem.PostId;
                }
            }
            return new PostChallengeProgressExtraResponse
            {
                Type = ChallengeTriggerEvent.POST.ToString(),
                QualifiedPostCount = qualifiedCount,
                LastQualifiedPostAt = lastQualifiedPostAt,
                LastPostId = lastPostId
            };
        }

        private static ReviewChallengeDetailProgressExtraResponse BuildReviewDetailExtra(CoupleChallengeProgressData progress, Dictionary<int, string>? memberNameMap = null)
        {
            var orderedQualifiedItems = progress.QualifiedItems?
                .Where(item =>
                    string.Equals(item.Type, ChallengeTriggerEvent.REVIEW.ToString(), StringComparison.OrdinalIgnoreCase)
                    && item.ReviewId.HasValue)
                .OrderByDescending(item => item.ActionAt)
                .ToList() ?? new List<QualifiedProgressItem>();

            var lastQualifiedItem = orderedQualifiedItems.FirstOrDefault();

            return new ReviewChallengeDetailProgressExtraResponse
            {
                Type = ChallengeTriggerEvent.REVIEW.ToString(),
                QualifiedReviewCount = orderedQualifiedItems.Count,
                LastQualifiedReviewAt = lastQualifiedItem?.ActionAt,

                LastVenueId = lastQualifiedItem?.VenueId,
                LastVenueName = lastQualifiedItem?.VenueName,

                RecentQualifiedItems = orderedQualifiedItems
                    .Select(item => new ReviewChallengeQualifiedItemResponse
                    {
                        ReviewId = item.ReviewId!.Value,
                        VenueId = item.VenueId ?? 0,
                        VenueName = item.VenueName,

                        MemberId = item.MemberId,
                        MemberName = memberNameMap != null &&
                                     memberNameMap.TryGetValue(item.MemberId, out var memberName)
                                     ? memberName
                                     : null,

                        QualifiedAt = item.ActionAt
                    })
                    .ToList()
            };
        }

        private static PostChallengeDetailProgressExtraResponse BuildPostDetailExtra(CoupleChallengeProgressData progress, Dictionary<int, string>? memberNameMap = null)
        {
            var orderedQualifiedItems = progress.QualifiedItems?
                .Where(item =>
                    string.Equals(item.Type, ChallengeTriggerEvent.POST.ToString(), StringComparison.OrdinalIgnoreCase)
                    && item.PostId.HasValue)
                .OrderByDescending(item => item.ActionAt)
                .ToList() ?? new List<QualifiedProgressItem>();

            var lastQualifiedItem = orderedQualifiedItems.FirstOrDefault();

            return new PostChallengeDetailProgressExtraResponse
            {
                Type = ChallengeTriggerEvent.POST.ToString(),
                QualifiedPostCount = orderedQualifiedItems.Count,
                LastQualifiedPostAt = lastQualifiedItem?.ActionAt,
                LastPostId = lastQualifiedItem?.PostId,

                RecentQualifiedItems = orderedQualifiedItems
                    .Select(item => new PostChallengeQualifiedItemResponse
                    {
                        PostId = item.PostId!.Value,
                        MemberId = item.MemberId,
                        MemberName = memberNameMap != null &&
                                     memberNameMap.TryGetValue(item.MemberId, out var memberName)
                                     ? memberName
                                     : null,
                        QualifiedAt = item.ActionAt
                    })
                    .ToList()
            };
        }
    }
}
