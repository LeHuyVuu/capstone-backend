namespace capstone_backend.Extensions.Common
{
    public static class CheckinBitMaskUtil
    {
        public static bool HasCheckin(int mask, int day)
        {
            ValidateDay(day);
            return (mask & (1 << (day - 1))) != 0;
        }

        public static int MarkCheckin(int mask, int day)
        {
            ValidateDay(day);
            return mask | (1 << (day - 1));
        }

        public static (int Current, int Best) CalculateStreak(int mask, int todayDay)
        {
            // 1. Calculate best streak
            int bestStreak = 0;
            int tempMask = mask;
            while (tempMask != 0)
            {
                tempMask &= (tempMask << 1);
                bestStreak++;
            }

            // 2. Calculate current streak
            int currentStreak = 0;
            int day = todayDay;

            while (day >= 1 && (mask & (1 << day - 1)) != 0)
            {
                currentStreak++;
                day--;
            }

            if (currentStreak == 0)
            {
                day = todayDay - 1;
                while (day >= 1 && (mask & (1 << (day - 1))) != 0)
                {
                    currentStreak++;
                    day--;
                }
            }

            return (currentStreak, bestStreak);
        }

        public static (int Current, int Best) CalculateCrossMonthStreak(
            Dictionary<string, Dictionary<string, int>> historyMonths,
            string key1,
            string? key2,
            DateOnly today)
        {
            int currentStreak = 0;
            int bestStreak = 0;

            if (historyMonths == null || !historyMonths.Any())
                return (0, 0);

            // 1. Sort months
            var sortedMonthKeys = historyMonths.Keys.OrderBy(k => k).ToList();
            DateOnly? lastProcessedDate = null;

            foreach (var monthKey in sortedMonthKeys)
            {
                // 2. Parse year-month
                var parts = monthKey.Split('-');
                if (parts.Length != 2 || !int.TryParse(parts[0], out int year) || !int.TryParse(parts[1], out int month))
                    continue;

                var memberMap = historyMonths[monthKey];
                int mask1 = memberMap != null && memberMap.TryGetValue(key1, out var m1) ? m1 : 0;

                int finalMask = mask1;
                if (key2 != null) // for couple
                {
                    int mask2 = memberMap != null && memberMap.TryGetValue(key2, out var m2) ? m2 : 0;
                    finalMask &= mask2;
                }

                int daysInMonth = DateTime.DaysInMonth(year, month);

                // 3. Interate months
                for (int day = 1; day <= daysInMonth; day++)
                {
                    var currentDate = new DateOnly(year, month, day);

                    // only till today
                    if (currentDate > today) break;

                    bool hasCheckin = (finalMask & (1 << (day - 1))) != 0;

                    if (hasCheckin)
                    {
                        // if break more than 1 day, reset streak
                        if (lastProcessedDate.HasValue && lastProcessedDate.Value.AddDays(1) < currentDate)
                        {
                            currentStreak = 0;
                        }

                        currentStreak++;
                        bestStreak = Math.Max(bestStreak, currentStreak);
                    }
                    else
                    {
                        // break streak if no checkin
                        currentStreak = 0;
                    }

                    lastProcessedDate = currentDate;
                }
            }

            return (currentStreak, bestStreak);
        }

        private static void ValidateDay(int day)
        {
            if (day < 1 || day > 31)
                throw new ArgumentOutOfRangeException(nameof(day), "Day must be between 1 and 31.");
        }
    }
}
